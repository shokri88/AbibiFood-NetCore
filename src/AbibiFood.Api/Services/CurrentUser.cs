using System.Security.Claims;
using AbibiFood.Application.Abstractions;
using AbibiFood.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace AbibiFood.Api.Services;

public sealed class CurrentUser(IHttpContextAccessor accessor, AppDbContext db) : IUserScope
{
    public string UserId => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    public bool IsManager => accessor.HttpContext?.User.IsInRole("Manager") == true;
    public async Task EnsureBranchAsync(int branchId, CancellationToken ct)
    {
        if (!await db.Branches.AnyAsync(x => x.Id == branchId, ct)) throw new ArgumentException("شعبه پیدا نشد.");
        if (IsManager) return;
        if (string.IsNullOrEmpty(UserId) || !await db.StaffBranches.AnyAsync(x => x.UserId == UserId && x.BranchId == branchId && x.IsActive, ct))
            throw new UnauthorizedAccessException("به این شعبه دسترسی ندارید.");
    }
}
