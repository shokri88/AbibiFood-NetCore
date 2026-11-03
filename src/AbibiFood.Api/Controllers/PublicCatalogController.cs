using AbibiFood.Application.Services;
using AbibiFood.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AbibiFood.Api.Controllers;

[ApiController, Route("api/catalog"), AllowAnonymous]
public sealed class PublicCatalogController(AppDbContext db) : ControllerBase
{
    [HttpGet("branches")]
    public async Task<IActionResult> Branches(CancellationToken ct) => Ok(await db.Branches.AsNoTracking().Where(x => x.IsActive)
        .Select(x => new { x.Id, x.Name, x.Address, x.Description, x.Latitude, x.Longitude }).ToListAsync(ct));
    [HttpGet("branches/{branchId:int}/menu")]
    public async Task<IActionResult> Menu(int branchId, DateOnly? date, CancellationToken ct)
    {
        if (!await db.Branches.AnyAsync(x => x.Id == branchId && x.IsActive, ct)) return NotFound();
        var day = date ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "Asia/Tehran").DateTime);
        var foods = await db.BranchFoods.AsNoTracking().Include(x => x.Food).ThenInclude(x => x.Images)
            .Where(x => x.BranchId == branchId && x.IsActive && x.Food.IsActive).ToListAsync(ct);
        var ids = foods.Select(x => x.Id).ToArray();
        var capacities = await CapacityReader.ReadAsync(db, branchId, day, day, ct);
        var groups = await db.BranchGroups.AsNoTracking().Where(x => x.BranchId == branchId && x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(ct);
        var groupIds = groups.Select(x => x.Id).ToArray();
        var memberships = await db.GroupFoods.AsNoTracking().Where(x => groupIds.Contains(x.BranchGroupId) && ids.Contains(x.BranchFoodId) && x.IsActive).ToListAsync(ct);
        var addons = await db.AllowedAddons.AsNoTracking().Where(x => ids.Contains(x.ParentBranchFoodId) && ids.Contains(x.AddonBranchFoodId) && x.IsActive).ToListAsync(ct);
        var hours = await db.BranchHours.AsNoTracking().Where(x => x.BranchId == branchId && x.IsActive).ToListAsync(ct);
        // فقط تعداد فروش ثبت‌شده؛ رزرو، ظرفیت یا ترتیب قالب معیار محبوبیت نیست.
        var today = AbibiFood.Domain.Rules.CapacitySchedule.Today();
        var standaloneIds = foods.Where(f => f.CanSellStandalone).Select(f => f.Id).ToArray();
        var sales = await db.DailyCapacities.AsNoTracking()
            .Where(c => standaloneIds.Contains(c.BranchFoodId) && c.Date <= today && c.Sold > 0)
            .GroupBy(c => c.BranchFoodId)
            .Select(g => new { BranchFoodId = g.Key, Sold = g.Sum(c => (long)c.Sold) })
            .OrderByDescending(g => g.Sold).ThenBy(g => g.BranchFoodId).Take(8).ToListAsync(ct);
        var bestSellerIds = sales.Select(s => s.BranchFoodId).ToArray();
        return Ok(new
        {
            branchId,
            date = day,
            currency = "TOMAN",
            orderingEnabled = false,
            bestSellerIds,
            groups,
            memberships,
            addons,
            hours,
            foods = foods.Select(x => new
            {
                x.Id,
                x.FoodId,
                x.Food.Name,
                x.Food.Title,
                x.Food.Description,
                x.Price,
                x.DiscountPrice,
                x.CanSellStandalone,
                images = x.Food.Images.Where(i => i.IsActive).OrderBy(i => i.SortOrder).ThenBy(i => i.Id).Select(i => i.Path),
                remaining = capacities.FirstOrDefault(c => c.BranchFoodId == x.Id && c.IsActive)?.Remaining ?? 0
            })
        });
    }
}
