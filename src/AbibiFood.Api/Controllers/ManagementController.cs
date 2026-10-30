using AbibiFood.Application.Abstractions;
using AbibiFood.Application.Models;
using AbibiFood.Application.Services;
using AbibiFood.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AbibiFood.Api.Controllers;

[ApiController, Route("api/manage"), Authorize(Policy = "ManagementApi"), IgnoreAntiforgeryToken]
public sealed class ManagementController(CatalogService service, AppDbContext db, IUserScope user) : ControllerBase
{
    [HttpPost("branches")]
    public async Task<IActionResult> SaveBranch(BranchInput input, CancellationToken ct) => Ok(new { id = await service.SaveBranchAsync(input, ct) });
    [HttpPost("foods")]
    public async Task<IActionResult> SaveFood(FoodInput input, CancellationToken ct) => Ok(new { id = await service.SaveFoodAsync(input, ct) });
    [HttpPost("images")]
    public async Task<IActionResult> SaveImage(ImageInput input, CancellationToken ct) => Ok(new { id = await service.SaveImageAsync(input, ct) });
    [HttpPost("groups")]
    public async Task<IActionResult> SaveGroup(GroupInput input, CancellationToken ct) => Ok(new { id = await service.SaveGroupAsync(input, ct) });
    [HttpPost("branch-foods")]
    public async Task<IActionResult> SaveBranchFood(BranchFoodInput input, CancellationToken ct) => Ok(new { id = await service.SaveBranchFoodAsync(input, ct) });
    [HttpPost("group-foods")]
    public async Task<IActionResult> SaveGroupFood(GroupFoodInput input, CancellationToken ct) => Ok(new { id = await service.SaveGroupFoodAsync(input, ct) });
    [HttpPost("addons")]
    public async Task<IActionResult> SaveAddon(AddonInput input, CancellationToken ct) => Ok(new { id = await service.SaveAddonAsync(input, ct) });
    [HttpPost("capacities")]
    public async Task<IActionResult> SaveCapacity(CapacityInput input, CancellationToken ct) => Ok(new { id = await service.SaveCapacityAsync(input, ct) });
    [HttpPost("hours")]
    public async Task<IActionResult> SaveHours(HoursInput input, CancellationToken ct) => Ok(new { id = await service.SaveHoursAsync(input, ct) });

    [HttpGet("branches/{branchId:int}/capacities")]
    public async Task<IActionResult> Capacities(int branchId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        await user.EnsureBranchAsync(branchId, ct);
        if (to < from || to.DayNumber - from.DayNumber > 366) return BadRequest();
        return Ok(await db.DailyCapacities.AsNoTracking().Where(c => c.Date >= from && c.Date <= to &&
            db.BranchFoods.Any(f => f.Id == c.BranchFoodId && f.BranchId == branchId)).ToListAsync(ct));
    }
}
