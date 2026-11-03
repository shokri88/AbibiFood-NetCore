using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AbibiFood.Domain.Entities;
using AbibiFood.Infrastructure.Identity;
using AbibiFood.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace AbibiFood.Tests.Integration;

[Collection("PhaseOneHttp")]
public sealed class PhaseOneHttpTests(PhaseOneFactory factory)
{
    private sealed record Actor(string Id, string Email, string Password, int BranchId, int OtherBranchId);
    private sealed record TokenResponse(string AccessToken);

    private async Task<Actor> CreateActor(string role)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync(role)) Assert.True((await roles.CreateAsync(new IdentityRole(role))).Succeeded);
        var email = Guid.NewGuid().ToString("N") + "@example.test";
        var password = "Abibi!1_" + Guid.NewGuid().ToString("N");
        var actor = new AppUser { UserName = email, Email = email, DisplayName = "کاربر آزمایشی" };
        Assert.True((await users.CreateAsync(actor, password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(actor, role)).Succeeded);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var first = new Branch { Name = "شعبه آزمایشی اول", Address = "آدرس نمونه اول" };
        var second = new Branch { Name = "شعبه آزمایشی دوم", Address = "آدرس نمونه دوم" };
        db.Branches.AddRange(first, second);
        await db.SaveChangesAsync();
        db.StaffBranches.Add(new StaffBranch { UserId = actor.Id, BranchId = first.Id });
        await db.SaveChangesAsync();
        return new Actor(actor.Id, email, password, first.Id, second.Id);
    }

    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    private async Task<string> Token(HttpClient client, Actor actor)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/token", new { actor.Email, actor.Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!.AccessToken;
    }

    private static string Capacities(int branchId)
    {
        var day = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1).ToString("yyyy-MM-dd");
        return $"/api/manage/branches/{branchId}/capacities?from={day}&to={day}";
    }

    [Fact]
    public async Task Employee_IsLimitedToAssignedBranch_AndCannotChangeCatalog()
    {
        var actor = await CreateActor("Employee"); using var client = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Capacities(actor.BranchId))).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await Token(client, actor));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Capacities(actor.BranchId))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Capacities(actor.OtherBranchId))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/manage/branches/{actor.BranchId}/capacity-defaults")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/manage/branches/{actor.OtherBranchId}/capacity-defaults")).StatusCode);
        using var forbidden = await client.PostAsJsonAsync("/api/manage/foods", new { Name = "غذا", Title = "عنوان غذا" });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task DisablingEmployee_RejectsAlreadyIssuedBearerToken()
    {
        var actor = await CreateActor("Employee"); using var client = Client();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await Token(client, actor));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Capacities(actor.BranchId))).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = (await users.FindByIdAsync(actor.Id))!; user.IsActive = false;
            Assert.True((await users.UpdateAsync(user)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Capacities(actor.BranchId))).StatusCode);
    }

    [Fact]
    public async Task RemovingManagerRole_RevokesPrivilegeOfExistingToken()
    {
        var actor = await CreateActor("Manager"); using var client = Client();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await Token(client, actor));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(Capacities(actor.OtherBranchId))).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = (await users.FindByIdAsync(actor.Id))!;
            Assert.True((await users.RemoveFromRoleAsync(user, "Manager")).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync(Capacities(actor.OtherBranchId))).StatusCode);
    }

    [Fact]
    public async Task RazorLogin_UsesCookie_AndRejectsPostWithoutAntiforgery()
    {
        var actor = await CreateActor("Manager"); using var client = Client();
        using var login = await client.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var html = await login.Content.ReadAsStringAsync();
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success);
        using var signedIn = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = actor.Email,
            ["Password"] = actor.Password,
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value)
        }));
        Assert.Equal(HttpStatusCode.Redirect, signedIn.StatusCode);
        // تمام صفحات پنل با Layout مشترک و تصاویر وابسته به غذا باید قابل رندر باشند.
        foreach (var page in new[] { "Index", "Branches", "Foods", "Groups", "Menu", "Memberships", "Addons", "Hours", "Capacities", "Employees", "Logs" })
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Admin/" + page)).StatusCode);
        int foodId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var food = new Food { Name = "غذای تصاویر", Title = "عنوان" };
            db.Foods.Add(food); await db.SaveChangesAsync(); foodId = food.Id;
        }
        using var images = await client.GetAsync("/Admin/Images?foodId=" + foodId);
        Assert.Equal(HttpStatusCode.OK, images.StatusCode);
        var imageHtml = await images.Content.ReadAsStringAsync();
        Assert.Contains("multipart/form-data", imageHtml);
        Assert.Contains("type=\"file\"", imageHtml);
        var imageToken = Regex.Match(imageHtml, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        using var invalidUpload = new MultipartFormDataContent();
        invalidUpload.Add(new StringContent(foodId.ToString()), "FoodId");
        invalidUpload.Add(new StringContent(WebUtility.HtmlDecode(imageToken.Groups[1].Value)), "__RequestVerificationToken");
        var fakeImage = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("<script>invalid</script>"));
        fakeImage.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        invalidUpload.Add(fakeImage, "imageFile", "fake.png");
        using var rejectedUpload = await client.PostAsync("/Admin/SaveImage", invalidUpload);
        Assert.Equal(HttpStatusCode.Redirect, rejectedUpload.StatusCode);
        Assert.StartsWith("/Admin/Images?", rejectedUpload.Headers.Location!.OriginalString);
        using (var scope = factory.Services.CreateScope())
            Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().FoodImages.AnyAsync(x => x.FoodId == foodId));

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Admin/Images?foodId=" + foodId + "&id=2147483647")).StatusCode);
        int branchFoodId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var offering = new BranchFood { FoodId = foodId, BranchId = actor.BranchId, Price = 20000, DiscountPrice = 15000, RowVersion = [1] };
            db.BranchFoods.Add(offering); await db.SaveChangesAsync(); branchFoodId = offering.Id;
            var group = new BranchGroup { BranchId = actor.BranchId, Title = "گروه آزمایش جدول", ImagePath = "/group-table.png", SortOrder = 7, IsActive = false };
            db.BranchGroups.Add(group); await db.SaveChangesAsync();
            db.GroupFoods.Add(new GroupFood { BranchGroupId = group.Id, BranchFoodId = offering.Id, SortOrder = 4, IsActive = false });
            await db.SaveChangesAsync();
        }
        foreach (var page in new[] { "Menu", "Groups", "Memberships", "Foods", "Branches", "Addons", "Hours", "Capacities", "Index", "Employees" })
        {
            var tableHtml = WebUtility.HtmlDecode(await client.GetStringAsync($"/Admin/{page}?branchId={actor.BranchId}"));
            Assert.Contains("<th>فعال</th>", tableHtml);
            if (page is "Groups" or "Memberships") Assert.Contains("<th>ترتیب</th>", tableHtml);
            if (page == "Menu")
            {
                Assert.Contains("<th>قیمت</th><th>قیمت تخفیفی</th>", tableHtml);
                Assert.DoesNotContain("<th>جزئیات</th>", tableHtml);
                Assert.Contains("غذای تصاویر</td>", tableHtml);
                Assert.Contains("۲۰,۰۰۰ تومان", tableHtml);
                Assert.Contains("۱۵,۰۰۰ تومان", tableHtml);
            }
            if (page == "Groups")
            {
                Assert.Contains("src=\"/group-table.png\"", tableHtml);
                Assert.Contains("data-sort=\"7\">7</td>", tableHtml);
                Assert.Contains("data-sort=\"0\">غیرفعال</td>", tableHtml);
            }
            if (page == "Memberships")
            {
                Assert.Contains("<th>گروه</th>", tableHtml);
                Assert.DoesNotContain("<th>جزئیات</th>", tableHtml);
                Assert.Contains("<td>گروه آزمایش جدول</td>", tableHtml);
                Assert.Contains("data-sort=\"4\">4</td>", tableHtml);
            }
        }
        foreach (var page in new[] { "Memberships", "Addons" })
        {
            var pageHtml = WebUtility.HtmlDecode(await client.GetStringAsync($"/Admin/{page}?branchId={actor.BranchId}"));
            Assert.Contains($"<option value=\"{branchFoodId}\">غذای تصاویر</option>", pageHtml);
            var emptyHtml = WebUtility.HtmlDecode(await client.GetStringAsync($"/Admin/{page}?branchId={actor.OtherBranchId}"));
            Assert.Contains("برای این شعبه غذایی تخصیص داده نشده", emptyHtml);
        }
        var capacityHtml = WebUtility.HtmlDecode(await client.GetStringAsync($"/Admin/Capacities?branchId={actor.BranchId}"));
        Assert.Contains("ظرفیت پیش‌فرض هر روز", capacityHtml);
        Assert.Contains($"<option value=\"{branchFoodId}\">غذای تصاویر</option>", capacityHtml);
        var capacityToken = Regex.Match(capacityHtml, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        var defaultsForm = new Dictionary<string, string>
        {
            ["BranchId"] = actor.BranchId.ToString(), ["Total"] = "۲۵", ["WarningThreshold"] = "۳", ["IsActive"] = "true",
            ["Foods[0].Selected"] = "true", ["Foods[0].BranchFoodId"] = AbibiFood.Api.Models.PersianDisplay.Digits(branchFoodId.ToString()),
            ["Foods[0].Version"] = "AQ=="
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Admin/SaveCapacityDefaults", new FormUrlEncodedContent(defaultsForm))).StatusCode);
        defaultsForm["__RequestVerificationToken"] = capacityToken;
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Admin/SaveCapacityDefaults", new FormUrlEncodedContent(defaultsForm))).StatusCode);
        foreach (var day in new[] { AbibiFood.Domain.Rules.CapacitySchedule.Today(), AbibiFood.Domain.Rules.CapacitySchedule.Today().AddDays(1) })
        {
            using var result = System.Text.Json.JsonDocument.Parse(await client.GetStringAsync($"/api/catalog/branches/{actor.BranchId}/menu?date={day:yyyy-MM-dd}"));
            var item = result.RootElement.GetProperty("foods").EnumerateArray().Single(f => f.GetProperty("id").GetInt32() == branchFoodId);
            Assert.Equal(25, item.GetProperty("remaining").GetInt32());
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.FoodImages.AddRange(
                new FoodImage { FoodId = foodId, Path = "/second.png", SortOrder = 1, IsActive = true },
                new FoodImage { FoodId = foodId, Path = "/main.png", SortOrder = 0, IsActive = true },
                new FoodImage { FoodId = foodId, Path = "/hidden.png", SortOrder = 0, IsActive = false });
            await db.SaveChangesAsync();
        }
        var foodsHtml = await client.GetStringAsync("/Admin/Foods");
        Assert.Contains("src=\"/main.png\"", foodsHtml);
        Assert.DoesNotContain("src=\"/hidden.png\"", foodsHtml);
        Assert.DoesNotContain("name=\"Title\"", foodsHtml);
        using var menuResponse = await client.GetAsync($"/api/catalog/branches/{actor.BranchId}/menu");
        var menuJson = System.Text.Json.JsonDocument.Parse(await menuResponse.Content.ReadAsStringAsync());
        var menuFood = menuJson.RootElement.GetProperty("foods").EnumerateArray().Single(x => x.GetProperty("id").GetInt32() == branchFoodId);
        Assert.Equal(new[] { "/main.png", "/second.png" }, menuFood.GetProperty("images").EnumerateArray().Select(x => x.GetString()));
        using var saveFood = await client.PostAsync("/Admin/SaveFood", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = foodId.ToString(), ["Name"] = "نام یکپارچه", ["IsActive"] = "true",
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(imageToken.Groups[1].Value)
        }));
        Assert.Equal(HttpStatusCode.Redirect, saveFood.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var food = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Foods.SingleAsync(x => x.Id == foodId);
            Assert.Equal("نام یکپارچه", food.Name); Assert.Equal(food.Name, food.Title);
        }
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        using var capacityPost = await client.PostAsync("/Admin/SaveCapacity", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["BranchFoodId"] = AbibiFood.Api.Models.PersianDisplay.Digits(branchFoodId.ToString()),
            ["branchId"] = actor.BranchId.ToString(),
            ["persianDate"] = AbibiFood.Api.Models.PersianDisplay.Date(tomorrow),
            ["Total"] = "۱۲", ["WarningThreshold"] = "۳", ["IsActive"] = "true",
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(imageToken.Groups[1].Value)
        }));
        Assert.Equal(HttpStatusCode.Redirect, capacityPost.StatusCode);
        int capacityId;
        using (var scope = factory.Services.CreateScope())
        {
            var capacity = await scope.ServiceProvider.GetRequiredService<AppDbContext>().DailyCapacities.SingleAsync(x => x.BranchFoodId == branchFoodId);
            Assert.Equal(tomorrow, capacity.Date); Assert.Equal(12, capacity.Total); capacityId = capacity.Id;
        }
        var deleteForm = new Dictionary<string, string>
        {
            ["kind"] = "DailyCapacity", ["id"] = capacityId.ToString(), ["returnUrl"] = "https://outside.example/"
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Admin/Delete", new FormUrlEncodedContent(deleteForm))).StatusCode);
        deleteForm["__RequestVerificationToken"] = WebUtility.HtmlDecode(imageToken.Groups[1].Value);
        using var deletion = await client.PostAsync("/Admin/Delete", new FormUrlEncodedContent(deleteForm));
        Assert.Equal(HttpStatusCode.Redirect, deletion.StatusCode);
        Assert.Equal("/", deletion.Headers.Location!.OriginalString);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await db.DailyCapacities.AnyAsync(x => x.Id == capacityId));
            Assert.True(await db.AuditLogs.AnyAsync(x => x.Action == "DeleteDailyCapacity" && x.EntityId == capacityId.ToString()));
        }
        deleteForm["kind"] = "Employee"; deleteForm["id"] = actor.Id;
        await client.PostAsync("/Admin/Delete", new FormUrlEncodedContent(deleteForm));
        using (var scope = factory.Services.CreateScope())
            Assert.NotNull(await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByIdAsync(actor.Id));

        using var post = await client.PostAsync("/Admin/SaveFood", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Name"] = "غذا",
            ["Title"] = "عنوان غذا"
        }));
        Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
    }
}
