using AbibiFood.Api.Models;
using AbibiFood.Api.Services;
using AbibiFood.Application.Models;
using AbibiFood.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AbibiFood.Api.Controllers;

public sealed partial class AdminController
{
    [Authorize(Roles = "Manager"), HttpGet]
    public async Task<IActionResult> Specials(int? branchId, int? id, CancellationToken ct)
    {
        var branch = await SelectBranch(branchId, ct); ViewData["BranchId"] = branch;
        ViewData["TableOptions"] = new AdminTableOptions(ShowDetails: false, ShowActive: true, ShowOrder: true);
        ViewData["BranchFoods"] = await db.BranchFoods.AsNoTracking().Include(f => f.Food).Where(f => f.BranchId == branch).OrderBy(f => f.Food.Name).ToListAsync(ct);
        var list = await db.BranchSpecials.AsNoTracking().Include(x => x.BranchFood).ThenInclude(f => f.Food).Where(x => x.BranchId == branch).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(ct);
        ViewData["Rows"] = list.Select(x => new AdminRow(x.BranchFood.Food.Name, x.BranchFood.Food.Name, $"/Admin/Specials?branchId={branch}&id={x.Id}", DeleteKind: nameof(BranchSpecial), DeleteId: x.Id, IsActive: x.IsActive, SortOrder: x.SortOrder)).ToList();
        var item = list.FirstOrDefault(x => x.Id == id);
        if (id.HasValue && item is null) return NotFound();
        return View(item is null ? new SpecialInput { BranchId = branch ?? 0 } : new SpecialInput { Id = item.Id, BranchId = item.BranchId, BranchFoodId = item.BranchFoodId, SortOrder = item.SortOrder, IsActive = item.IsActive });
    }
    [Authorize(Roles = "Manager"), HttpPost]
    public async Task<IActionResult> SaveSpecial(SpecialInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await service.SaveSpecialAsync(input, ct); TempData["Success"] = messages["Saved"];
        return RedirectToAction(nameof(Specials), new { branchId = input.BranchId });
    }
    [Authorize(Roles = "Manager"), HttpGet]
    public async Task<IActionResult> Banners(int? branchId, int? id, CancellationToken ct)
    {
        var branch = await SelectBranch(branchId, ct); ViewData["BranchId"] = branch;
        ViewData["TableOptions"] = new AdminTableOptions(DetailsTitle: "غذای مقصد", ShowActive: true, ShowOrder: true);
        ViewData["BranchFoods"] = await db.BranchFoods.AsNoTracking().Include(f => f.Food).Where(f => f.BranchId == branch).OrderBy(f => f.Food.Name).ToListAsync(ct);
        var list = await db.BranchBanners.AsNoTracking().Include(x => x.BranchFood).ThenInclude(f => f.Food).Where(x => x.BranchId == branch).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync(ct);
        ViewData["Rows"] = list.Select(x => new AdminRow(x.Title, x.BranchFood.Food.Name, $"/Admin/Banners?branchId={branch}&id={x.Id}", DeleteKind: nameof(BranchBanner), DeleteId: x.Id, ThumbnailUrl: x.ImagePath, IsActive: x.IsActive, SortOrder: x.SortOrder)).ToList();
        var item = list.FirstOrDefault(x => x.Id == id);
        if (id.HasValue && item is null) return NotFound();
        return View(item is null ? new BannerInput { BranchId = branch ?? 0 } : new BannerInput { Id = item.Id, BranchId = item.BranchId, BranchFoodId = item.BranchFoodId, Title = item.Title, Text = item.Text, ImagePath = item.ImagePath, SortOrder = item.SortOrder, IsActive = item.IsActive });
    }
    [Authorize(Roles = "Manager"), HttpPost, RequestSizeLimit(ImageUploadStore.MaxRequestBytes)]
    public async Task<IActionResult> SaveBanner(BannerInput input, IFormFile? imageFile, CancellationToken ct)
    {
        await user.EnsureBranchAsync(input.BranchId, ct);
        var existing = input.Id == 0 ? null : await db.BranchBanners.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.Id && x.BranchId == input.BranchId, ct);
        if (input.Id != 0 && existing is null) return NotFound();
        input.ImagePath = existing?.ImagePath ?? ""; // مسیر ارسالی فرم قابل اعتماد نیست.
        if (!ModelState.IsValid) return BadRequest(ModelState);
        string? newPath = null;
        try
        {
            if (imageFile is not null) newPath = await uploads.SaveAsync(imageFile, ct);
            input.ImagePath = newPath ?? input.ImagePath;
            await service.SaveBannerAsync(input, ct);
        }
        catch (ArgumentException error)
        {
            if (newPath is not null) uploads.DeleteNewFile(newPath);
            TempData["Error"] = error.Message;
            return RedirectToAction(nameof(Banners), new { branchId = input.BranchId, id = input.Id == 0 ? (int?)null : input.Id });
        }
        catch { if (newPath is not null) uploads.DeleteNewFile(newPath); throw; }
        TempData["Success"] = messages["Saved"];
        return RedirectToAction(nameof(Banners), new { branchId = input.BranchId });
    }
}
