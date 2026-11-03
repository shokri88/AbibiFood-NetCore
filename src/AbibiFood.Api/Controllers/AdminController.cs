using AbibiFood.Api.Models;
using AbibiFood.Api.Services;
using AbibiFood.Application.Abstractions;
using AbibiFood.Application.Models;
using AbibiFood.Application.Services;
using AbibiFood.Domain.Entities;
using AbibiFood.Infrastructure.Identity;
using AbibiFood.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AbibiFood.Api.Controllers;

[Authorize(Roles = "Manager,Employee")]
public sealed class AdminController(AppDbContext db, CatalogService service, IUserScope user, Messages messages, UserManager<AppUser> users, ImageUploadStore uploads) : Controller
{
    private async Task<int?> SelectBranch(int? branchId, CancellationToken ct)
    {
        var query = db.Branches.AsNoTracking();
        if (!user.IsManager) query = query.Where(b => db.StaffBranches.Any(s => s.UserId == user.UserId && s.BranchId == b.Id && s.IsActive));
        var branches = await query.OrderBy(b => b.Name).ToListAsync(ct); ViewData["Branches"] = branches;
        if (branchId.HasValue) { await user.EnsureBranchAsync(branchId.Value, ct); return branchId; }
        return branches.FirstOrDefault()?.Id;
    }
    [HttpGet]
    public async Task<IActionResult> Index(int? branchId, CancellationToken ct)
    {
        var branch = await SelectBranch(branchId, ct); ViewData["BranchId"] = branch;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Asia/Tehran").DateTime);
        var capacities = branch.HasValue ? await CapacityReader.ReadAsync(db, branch.Value, today, today, ct) : [];
        ViewData["BranchFoods"] = await db.BranchFoods.AsNoTracking().Include(f => f.Food).Where(f => f.BranchId == branch).ToListAsync(ct);
        return View(capacities);
    }
    [Authorize(Roles = "Manager"), HttpGet]
    public async Task<IActionResult> Branches(int? id, CancellationToken ct)
    {
        ViewData["TableOptions"] = new AdminTableOptions(ShowActive: true);
        var list = await db.Branches.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);
        ViewData["Rows"] = list.Select(x => new AdminRow(x.Name, x.Address, "/Admin/Branches?id=" + x.Id, DeleteKind: "Branch", DeleteId: x.Id, IsActive: x.IsActive)).ToList();
        var x = list.FirstOrDefault(x => x.Id == id);
        return View(x is null ? new BranchInput() : new BranchInput { Id = x.Id, Name = x.Name, Address = x.Address, Description = x.Description, Latitude = x.Latitude, Longitude = x.Longitude, IsActive = x.IsActive });
    }
    [Authorize(Roles = "Manager"), HttpPost]
    public async Task<IActionResult> SaveBranch(BranchInput input, CancellationToken ct)
    { if (!ModelState.IsValid) return BadRequest(ModelState); await service.SaveBranchAsync(input, ct); TempData["Success"] = messages["Saved"]; return RedirectToAction(nameof(Branches)); }
    [Authorize(Roles = "Manager"), HttpGet]
    public async Task<IActionResult> Foods(int? id, CancellationToken ct)
    {
        ViewData["TableOptions"] = new AdminTableOptions(ShowActive: true);
        var list = await db.Foods.AsNoTracking().Include(x => x.Images).OrderBy(x => x.Name).ToListAsync(ct);
        ViewData["Rows"] = list.Select(x => new AdminRow(x.Name, x.Description, "/Admin/Foods?id=" + x.Id, DeleteKind: "Food", DeleteId: x.Id, IsActive: x.IsActive, ImageUrl: "/Admin/Images?foodId=" + x.Id, ThumbnailUrl: x.Images.Where(i => i.IsActive && i.SortOrder == 0).OrderBy(i => i.Id).Select(i => i.Path).FirstOrDefault())).ToList();
        var x = list.FirstOrDefault(x => x.Id == id); return View(x is null ? new FoodInput() : new FoodInput { Id = x.Id, Name = x.Name, Title = x.Title, Description = x.Description, IsActive = x.IsActive });
    }
    [Authorize(Roles = "Manager"), HttpPost]
    public async Task<IActionResult> SaveFood(FoodInput input, CancellationToken ct)
    { if (!ModelState.IsValid) return BadRequest(ModelState); var id = await service.SaveFoodAsync(input, ct); TempData["Success"] = messages["Saved"]; return RedirectToAction(nameof(Foods), new { id }); }
    [Authorize(Roles = "Manager"), HttpGet]
    public async Task<IActionResult> Images(int? foodId, int? id, CancellationToken ct)
    {
        var image = id.HasValue ? await db.FoodImages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) : null;
        var selectedFood = foodId ?? image?.FoodId;
        if (!selectedFood.HasValue) return RedirectToAction(nameof(Foods));
        var food = await db.Foods.AsNoTracking().SingleOrDefaultAsync(x => x.Id == selectedFood, ct);
        if (food is null || (id.HasValue && (image is null || image.FoodId != food.Id))) return NotFound();
        ViewData["FoodName"] = food.Name;
        ViewData["Images"] = await db.FoodImages.AsNoTracking().Where(x => x.FoodId == food.Id).OrderBy(x => x.SortOrder).ToListAsync(ct);
        return View(image is null ? new ImageInput { FoodId = food.Id } : new ImageInput
        { Id = image.Id, FoodId = image.FoodId, Path = image.Path, SortOrder = image.SortOrder, IsActive = image.IsActive });
    }
    [Authorize(Roles = "Manager"), HttpPost, RequestSizeLimit(ImageUploadStore.MaxRequestBytes)]
    public async Task<IActionResult> SaveImage(ImageInput input, IFormFile? imageFile, CancellationToken ct)
    {
        var existing = input.Id == 0 ? null : await db.FoodImages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.Id, ct);
        if (!await db.Foods.AnyAsync(x => x.Id == input.FoodId, ct) ||
            (input.Id != 0 && (existing is null || existing.FoodId != input.FoodId))) return NotFound();
        // مسیر از فایل جدید یا رکورد فعلی خوانده می‌شود، نه hidden input قابل دست‌کاری.
        input.Path = existing?.Path ?? "";
        ModelState.Remove(nameof(input.Path));
        if (!ModelState.IsValid) return BadRequest(ModelState);
        string? newPath = null;
        try
        {
            if (imageFile is not null) newPath = await uploads.SaveAsync(imageFile, ct);
            if (newPath is null && existing is null) throw new ArgumentException(messages["ImageRequired"]);
            input.Path = newPath ?? input.Path;
            await service.SaveImageAsync(input, ct);
        }
        catch (ArgumentException error)
        {
            if (newPath is not null) uploads.DeleteNewFile(newPath);
            TempData["Error"] = error.Message;
            return RedirectToAction(nameof(Images), new { foodId = input.FoodId, id = input.Id == 0 ? (int?)null : input.Id });
        }
        catch { if (newPath is not null) uploads.DeleteNewFile(newPath); throw; }
        TempData["Success"] = messages["Saved"];
        return RedirectToAction(nameof(Images), new { foodId = input.FoodId });
    }
    [Authorize(Roles = "Manager"), HttpGet]
    public async Task<IActionResult> Groups(int? branchId, int? id, CancellationToken ct)
    {
        ViewData["TableOptions"] = new AdminTableOptions(ShowDetails: false, ShowActive: true, ShowOrder: true);
        var branch = await SelectBranch(branchId, ct); ViewData["BranchId"] = branch;
        ViewData["Foods"] = await db.Foods.AsNoTracking().ToListAsync(ct);
        ViewData["BranchFoods"] = await db.BranchFoods.AsNoTracking().Include(x => x.Food).Where(x => x.BranchId == branch).ToListAsync(ct);
        ViewData["Groups"] = await db.BranchGroups.AsNoTracking().Where(x => x.BranchId == branch).ToListAsync(ct);
        var list = await db.BranchGroups.AsNoTracking().Where(x => x.BranchId == branch).ToListAsync(ct);
        ViewData["Rows"] = list.Select(x => new AdminRow(x.Title, "", "/Admin/Groups?branchId=" + branch + "&id=" + x.Id, DeleteKind: "BranchGroup", DeleteId: x.Id, IsActive: x.IsActive, SortOrder: x.SortOrder, ThumbnailUrl: string.IsNullOrWhiteSpace(x.ImagePath) ? null : x.ImagePath)).ToList();
        var x = list.FirstOrDefault(x => x.Id == id); return View(x is null ? new GroupInput { BranchId = branch ?? 0 } : new GroupInput { Id = x.Id, BranchId = x.BranchId, Title = x.Title, ImagePath = x.ImagePath, SortOrder = x.SortOrder, IsActive = x.IsActive });
    }
    [Authorize(Roles = "Manager"), HttpPost, RequestSizeLimit(ImageUploadStore.MaxRequestBytes)]
    public async Task<IActionResult> SaveGroup(GroupInput input, int? branchId, IFormFile? imageFile, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await user.EnsureBranchAsync(input.BranchId, ct);
        var existing = input.Id == 0 ? null : await db.BranchGroups.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.Id && x.BranchId == input.BranchId, ct);
        if (input.Id != 0 && existing is null) return NotFound();
        input.ImagePath = existing?.ImagePath ?? "";
        string? newPath = null;
        try
        {
            if (imageFile is not null) { newPath = await uploads.SaveAsync(imageFile, ct); input.ImagePath = newPath; }
            await service.SaveGroupAsync(input, ct);
        }
        catch (ArgumentException error)
        {
            if (newPath is not null) uploads.DeleteNewFile(newPath);
            TempData["Error"] = error.Message;
            return RedirectToAction(nameof(Groups), new { branchId = input.BranchId, id = input.Id });
        }
        catch { if (newPath is not null) uploads.DeleteNewFile(newPath); throw; }
        TempData["Success"] = messages["Saved"];
        return RedirectToAction(nameof(Groups), new { branchId = input.BranchId });
    }
    [Authorize(Roles = "Manager"), HttpGet]
    public async Task<IActionResult> Menu(int? branchId, int? id, CancellationToken ct)
    {
        ViewData["TableOptions"] = new AdminTableOptions(ShowDetails: false, ShowPrices: true, ShowActive: true);
        var branch = await SelectBranch(branchId, ct); ViewData["BranchId"] = branch;
        ViewData["Foods"] = await db.Foods.AsNoTracking().ToListAsync(ct);
        ViewData["BranchFoods"] = await db.BranchFoods.AsNoTracking().Include(x => x.Food).Where(x => x.BranchId == branch).ToListAsync(ct);
        ViewData["Groups"] = await db.BranchGroups.AsNoTracking().Where(x => x.BranchId == branch).ToListAsync(ct);
        var list = await db.BranchFoods.AsNoTracking().Include(x => x.Food).Where(x => x.BranchId == branch).ToListAsync(ct);
        ViewData["Rows"] = list.Select(x => new AdminRow(x.Food.Name, "", "/Admin/Menu?branchId=" + branch + "&id=" + x.Id, DeleteKind: "BranchFood", DeleteId: x.Id, IsActive: x.IsActive, Price: x.Price, DiscountPrice: x.DiscountPrice)).ToList();
        var x = list.FirstOrDefault(x => x.Id == id); return View(x is null ? new BranchFoodInput { BranchId = branch ?? 0 } : new BranchFoodInput { Id = x.Id, BranchId = x.BranchId, FoodId = x.FoodId, Price = x.Price, DiscountPrice = x.DiscountPrice, CanSellStandalone = x.CanSellStandalone, IsActive = x.IsActive, Version = Convert.ToBase64String(x.RowVersion) });
    }
    [Authorize(Roles = "Manager"), HttpPost]
    public async Task<IActionResult> SaveBranchFood(BranchFoodInput input, int? branchId, CancellationToken ct)
    { if (!ModelState.IsValid) return BadRequest(ModelState); await service.SaveBranchFoodAsync(input, ct); return RedirectToAction(nameof(Menu), new { branchId }); }
    [Authorize(Roles = "Manager"), HttpGet]
    public async Task<IActionResult> Memberships(int? branchId, int? id, CancellationToken ct)
    {
        ViewData["TableOptions"] = new AdminTableOptions(DetailsTitle: "گروه", ShowActive: true, ShowOrder: true);
        var branch = await SelectBranch(branchId, ct); ViewData["BranchId"] = branch;
        ViewData["Foods"] = await db.Foods.AsNoTracking().ToListAsync(ct);
        ViewData["BranchFoods"] = await db.BranchFoods.AsNoTracking().Include(x => x.Food).Where(x => x.BranchId == branch).ToListAsync(ct);
        ViewData["Groups"] = await db.BranchGroups.AsNoTracking().Where(x => x.BranchId == branch).ToListAsync(ct);
        var list = await db.GroupFoods.AsNoTracking().Where(x => db.BranchFoods.Any(f => f.Id == x.BranchFoodId && f.BranchId == branch)).ToListAsync(ct);
        ViewData["Rows"] = list.Select(x => new AdminRow(((List<BranchFood>)ViewData["BranchFoods"]!).Single(f => f.Id == x.BranchFoodId).Food.Name, ((List<BranchGroup>)ViewData["Groups"]!).Single(g => g.Id == x.BranchGroupId).Title, "/Admin/Memberships?branchId=" + branch + "&id=" + x.Id, DeleteKind: "GroupFood", DeleteId: x.Id, IsActive: x.IsActive, SortOrder: x.SortOrder)).ToList();
        var x = list.FirstOrDefault(x => x.Id == id); return View(x is null ? new GroupFoodInput() : new GroupFoodInput { Id = x.Id, BranchGroupId = x.BranchGroupId, BranchFoodId = x.BranchFoodId, SortOrder = x.SortOrder, IsActive = x.IsActive });
    }
    [Authorize(Roles = "Manager"), HttpPost]
    public async Task<IActionResult> SaveGroupFood(GroupFoodInput input, int? branchId, CancellationToken ct)
    { if (!ModelState.IsValid) return BadRequest(ModelState); await service.SaveGroupFoodAsync(input, ct); return RedirectToAction(nameof(Memberships), new { branchId }); }
    [Authorize(Roles = "Manager"), HttpGet]
    public async Task<IActionResult> Addons(int? branchId, int? id, CancellationToken ct)
    {
        ViewData["TableOptions"] = new AdminTableOptions(DetailsTitle: "افزودنی", ShowActive: true);
        var branch = await SelectBranch(branchId, ct); ViewData["BranchId"] = branch;
        ViewData["Foods"] = await db.Foods.AsNoTracking().ToListAsync(ct);
        ViewData["BranchFoods"] = await db.BranchFoods.AsNoTracking().Include(x => x.Food).Where(x => x.BranchId == branch).ToListAsync(ct);
        ViewData["Groups"] = await db.BranchGroups.AsNoTracking().Where(x => x.BranchId == branch).ToListAsync(ct);
        var list = await db.AllowedAddons.AsNoTracking().Where(x => db.BranchFoods.Any(f => f.Id == x.ParentBranchFoodId && f.BranchId == branch)).ToListAsync(ct);
        ViewData["Rows"] = list.Select(x => new AdminRow(((List<BranchFood>)ViewData["BranchFoods"]!).Single(f => f.Id == x.ParentBranchFoodId).Food.Name, ((List<BranchFood>)ViewData["BranchFoods"]!).Single(f => f.Id == x.AddonBranchFoodId).Food.Name, "/Admin/Addons?branchId=" + branch + "&id=" + x.Id, DeleteKind: "AllowedAddon", DeleteId: x.Id, IsActive: x.IsActive)).ToList();
        var x = list.FirstOrDefault(x => x.Id == id); return View(x is null ? new AddonInput() : new AddonInput { Id = x.Id, ParentBranchFoodId = x.ParentBranchFoodId, AddonBranchFoodId = x.AddonBranchFoodId, MaxQuantity = x.MaxQuantity, IsActive = x.IsActive });
    }
    [Authorize(Roles = "Manager"), HttpPost]
    public async Task<IActionResult> SaveAddon(AddonInput input, int? branchId, CancellationToken ct)
    { if (!ModelState.IsValid) return BadRequest(ModelState); await service.SaveAddonAsync(input, ct); return RedirectToAction(nameof(Addons), new { branchId }); }
    [Authorize(Roles = "Manager"), HttpGet]
    public async Task<IActionResult> Hours(int? branchId, int? id, CancellationToken ct)
    {
        ViewData["TableOptions"] = new AdminTableOptions(ShowActive: true);
        var branch = await SelectBranch(branchId, ct); ViewData["BranchId"] = branch;
        ViewData["Foods"] = await db.Foods.AsNoTracking().ToListAsync(ct);
        ViewData["BranchFoods"] = await db.BranchFoods.AsNoTracking().Include(x => x.Food).Where(x => x.BranchId == branch).ToListAsync(ct);
        ViewData["Groups"] = await db.BranchGroups.AsNoTracking().Where(x => x.BranchId == branch).ToListAsync(ct);
        var list = await db.BranchHours.AsNoTracking().Where(x => x.BranchId == branch).ToListAsync(ct);
        ViewData["Rows"] = list.Select(x => new AdminRow(PersianDisplay.Day(x.Day) + " · " + PersianDisplay.Hours(x.Kind), x.Start + " تا " + x.End, "/Admin/Hours?branchId=" + branch + "&id=" + x.Id, DeleteKind: "BranchHours", DeleteId: x.Id, IsActive: x.IsActive)).ToList();
        var x = list.FirstOrDefault(x => x.Id == id); return View(x is null ? new HoursInput { BranchId = branch ?? 0 } : new HoursInput { Id = x.Id, BranchId = x.BranchId, Kind = x.Kind, Day = x.Day, Start = x.Start, End = x.End, IsActive = x.IsActive });
    }
    [Authorize(Roles = "Manager"), HttpPost]
    public async Task<IActionResult> SaveHours(HoursInput input, int? branchId, CancellationToken ct)
    { if (!ModelState.IsValid) return BadRequest(ModelState); await service.SaveHoursAsync(input, ct); return RedirectToAction(nameof(Hours), new { branchId }); }
    [HttpGet]
    public async Task<IActionResult> Capacities(int? branchId, int? foodId, DateOnly? date, CancellationToken ct)
    {
        ViewData["TableOptions"] = new AdminTableOptions(ShowActive: true);
        var branch = await SelectBranch(branchId, ct); ViewData["BranchId"] = branch;
        var foods = await db.BranchFoods.AsNoTracking().Include(x => x.Food).Where(x => x.BranchId == branch).ToListAsync(ct); ViewData["BranchFoods"] = foods;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Asia/Tehran").DateTime);
        ViewData["EffectiveCapacities"] = branch.HasValue ? await CapacityReader.ReadAsync(db, branch.Value, today, today, ct) : new List<DailyCapacity>();
        var selected = date ?? today; var ids = foods.Select(x => x.Id).ToArray();
        var list = await db.DailyCapacities.AsNoTracking().Where(x => ids.Contains(x.BranchFoodId) && x.Date >= today).OrderBy(x => x.Date).ToListAsync(ct);
        ViewData["Rows"] = list.Select(x => new AdminRow(foods.Single(f => f.Id == x.BranchFoodId).Food.Name + " · " + PersianDisplay.Date(x.Date),
            "کل " + x.Total + " · باقی‌مانده " + x.Remaining + (x.Remaining <= x.WarningThreshold ? " · رو به اتمام" : ""), "/Admin/Capacities?branchId=" + branch + "&foodId=" + x.BranchFoodId + "&date=" + x.Date.ToString("yyyy-MM-dd"), DeleteKind: "DailyCapacity", DeleteId: x.Id, IsActive: x.IsActive)).ToList();
        var c = list.FirstOrDefault(x => x.BranchFoodId == foodId && x.Date == selected);
        return View(c is null ? new CapacityInput { Date = selected } : new CapacityInput { BranchFoodId = c.BranchFoodId, Date = c.Date, Total = c.Total, WarningThreshold = c.WarningThreshold, IsActive = c.IsActive, Version = Convert.ToBase64String(c.RowVersion) });
    }
    [HttpPost, RequestFormLimits(ValueCountLimit = 5000)]
    public async Task<IActionResult> SaveCapacityDefaults(CapacityDefaultsInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await service.SaveCapacityDefaultsAsync(input, ct);
        TempData["Success"] = messages["Saved"];
        return RedirectToAction(nameof(Capacities), new { branchId = input.BranchId });
    }
    [HttpPost]
    public async Task<IActionResult> SaveCapacity(CapacityInput input, int? branchId, string persianDate, CancellationToken ct)
    {
        if (!PersianDisplay.TryDate(persianDate, out var date))
        {
            TempData["Error"] = messages["InvalidPersianDate"];
            return RedirectToAction(nameof(Capacities), new { branchId });
        }
        input.Date = date;
        ModelState.Remove(nameof(input.Date));
        if (!ModelState.IsValid) return BadRequest(ModelState); await service.SaveCapacityAsync(input, ct); return RedirectToAction(nameof(Capacities), new { branchId }); }
    [Authorize(Roles = "Manager"), HttpGet]
    public async Task<IActionResult> Employees(CancellationToken ct)
    {
        ViewData["Branches"] = await db.Branches.AsNoTracking().ToListAsync(ct);
        ViewData["Staff"] = await db.StaffBranches.AsNoTracking().ToListAsync(ct);
        return View((await users.GetUsersInRoleAsync("Employee")).ToList());
    }
    [Authorize(Roles = "Manager"), HttpPost]
    public async Task<IActionResult> CreateEmployee(string email, string password, string displayName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return BadRequest("نام، ایمیل و رمز لازم است.");
        var employee = new AppUser { UserName = email, Email = email, DisplayName = displayName };
        var result = await users.CreateAsync(employee, password); if (!result.Succeeded) return BadRequest(string.Join("؛ ", result.Errors.Select(x => x.Description)));
        var assigned = await users.AddToRoleAsync(employee, "Employee"); if (!assigned.Succeeded) throw new InvalidOperationException("اختصاص نقش ناموفق بود.");
        db.AuditLogs.Add(new AuditLog { ActorId = user.UserId, Action = "CreateEmployee", EntityType = "AppUser", EntityId = employee.Id }); await db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Employees));
    }
    [Authorize(Roles = "Manager"), HttpPost]
    public async Task<IActionResult> AssignBranch(string userId, int branchId, bool isActive, CancellationToken ct)
    {
        await user.EnsureBranchAsync(branchId, ct); var employee = await users.FindByIdAsync(userId);
        if (employee is null || !await users.IsInRoleAsync(employee, "Employee")) return BadRequest("کارمند پیدا نشد.");
        var mapping = await db.StaffBranches.SingleOrDefaultAsync(x => x.UserId == userId && x.BranchId == branchId, ct);
        if (mapping is null) { mapping = new StaffBranch { UserId = userId, BranchId = branchId }; db.StaffBranches.Add(mapping); }
        mapping.IsActive = isActive;
        db.AuditLogs.Add(new AuditLog { ActorId = user.UserId, BranchId = branchId, Action = "AssignBranch", EntityType = "StaffBranch", EntityId = userId }); await db.SaveChangesAsync(ct); return RedirectToAction(nameof(Employees));
    }
    [Authorize(Roles = "Manager"), HttpPost]
    public async Task<IActionResult> SetEmployeeActive(string userId, bool isActive, CancellationToken ct)
    {
        var employee = await users.FindByIdAsync(userId);
        if (employee is null || !await users.IsInRoleAsync(employee, "Employee") || await users.IsInRoleAsync(employee, "Manager"))
            return BadRequest("فقط وضعیت کارمند قابل تغییر است.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        employee.IsActive = isActive;
        var result = await users.UpdateAsync(employee);
        if (!result.Succeeded) throw new InvalidOperationException("ذخیره وضعیت کارمند ناموفق بود.");
        db.AuditLogs.Add(new AuditLog { ActorId = user.UserId, Action = isActive ? "EnableEmployee" : "DisableEmployee", EntityType = "AppUser", EntityId = employee.Id });
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
        return RedirectToAction(nameof(Employees));
    }
    [Authorize(Roles = "Manager"), HttpGet]
    public async Task<IActionResult> Logs(CancellationToken ct)
    { ViewData["Errors"] = await db.ErrorLogs.AsNoTracking().OrderByDescending(x => x.Id).Take(100).ToListAsync(ct); return View(await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.Id).Take(100).ToListAsync(ct)); }

    [HttpPost]
    public async Task<IActionResult> Delete(string kind, string id, string? returnUrl, CancellationToken ct)
    {
        var target = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index))!;
        try
        {
            if (kind == "Employee")
            {
                if (!user.IsManager) return Forbid();
                var employee = await users.FindByIdAsync(id);
                if (employee is null || !await users.IsInRoleAsync(employee, "Employee") || await users.IsInRoleAsync(employee, "Manager"))
                    throw new ArgumentException(messages["DeleteEmployeeOnly"]);
                if (await db.StaffBranches.AnyAsync(x => x.UserId == id, ct)) throw new ArgumentException(messages["EmployeeHasBranches"]);
                await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(ct) : null;
                var result = await users.DeleteAsync(employee);
                if (!result.Succeeded) throw new ArgumentException(messages["DeleteFailed"]);
                db.AuditLogs.Add(new AuditLog { ActorId = user.UserId, Action = "DeleteEmployee", EntityType = "AppUser", EntityId = id });
                await db.SaveChangesAsync(ct);
                if (transaction is not null) await transaction.CommitAsync(ct);
            }
            else
            {
                if (!int.TryParse(id, out var entityId)) throw new ArgumentException(messages["DeleteFailed"]);
                await service.DeleteAsync(kind, entityId, ct);
            }
            TempData["Success"] = messages["Deleted"];
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException error) { TempData["Error"] = error.Message; }
        catch (DbUpdateConcurrencyException) { TempData["Error"] = messages["DeleteConflict"]; }
        catch (DbUpdateException error) when (error.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 })
        { TempData["Error"] = messages["DeleteHasDependencies"]; }
        return LocalRedirect(target);
    }
}
