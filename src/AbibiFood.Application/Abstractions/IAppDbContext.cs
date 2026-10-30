using AbibiFood.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace AbibiFood.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Branch> Branches { get; }
    DbSet<Food> Foods { get; }
    DbSet<FoodImage> FoodImages { get; }
    DbSet<BranchGroup> BranchGroups { get; }
    DbSet<BranchFood> BranchFoods { get; }
    DbSet<GroupFood> GroupFoods { get; }
    DbSet<AllowedAddon> AllowedAddons { get; }
    DbSet<DailyCapacity> DailyCapacities { get; }
    DbSet<BranchHours> BranchHours { get; }
    DbSet<StaffBranch> StaffBranches { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ErrorLog> ErrorLogs { get; }
    Task<int> SaveWithAuditAsync(AuditLog audit, object entity, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
