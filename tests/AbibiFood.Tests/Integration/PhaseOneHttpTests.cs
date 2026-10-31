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
        using var post = await client.PostAsync("/Admin/SaveFood", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Name"] = "غذا",
            ["Title"] = "عنوان غذا"
        }));
        Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
    }
}
