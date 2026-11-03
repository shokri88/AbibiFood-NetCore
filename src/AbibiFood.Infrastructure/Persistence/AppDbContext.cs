using AbibiFood.Application.Abstractions;
using AbibiFood.Domain.Entities;
using AbibiFood.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace AbibiFood.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options), IAppDbContext
{
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<BranchSpecial> BranchSpecials => Set<BranchSpecial>();
    public DbSet<BranchBanner> BranchBanners => Set<BranchBanner>();
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<FoodImage> FoodImages => Set<FoodImage>();
    public DbSet<BranchGroup> BranchGroups => Set<BranchGroup>();
    public DbSet<BranchFood> BranchFoods => Set<BranchFood>();
    public DbSet<GroupFood> GroupFoods => Set<GroupFood>();
    public DbSet<AllowedAddon> AllowedAddons => Set<AllowedAddon>();
    public DbSet<DailyCapacity> DailyCapacities => Set<DailyCapacity>();
    public DbSet<BranchHours> BranchHours => Set<BranchHours>();
    public DbSet<StaffBranch> StaffBranches => Set<StaffBranch>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    public async Task<int> SaveWithAuditAsync(AuditLog audit, object entity, CancellationToken ct)
    {
        // شناسه تولیدشده ابتدا دریافت می‌شود؛ هر دو ذخیره داخل یک تراکنش SQL هستند.
        await using var transaction = Database.IsRelational() ? await Database.BeginTransactionAsync(ct) : null;
        var changed = await base.SaveChangesAsync(ct);
        audit.EntityId = Entry(entity).Property("Id").CurrentValue?.ToString() ?? "";
        AuditLogs.Add(audit);
        await base.SaveChangesAsync(ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
        return changed;
    }
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Branch>().Property(x => x.Latitude).HasPrecision(10, 7);
        b.Entity<Branch>().Property(x => x.Longitude).HasPrecision(10, 7);
        b.Entity<BranchFood>().Property(x => x.Price).HasPrecision(18, 2);
        b.Entity<BranchFood>().Property(x => x.DiscountPrice).HasPrecision(18, 2);
        b.Entity<BranchFood>().Property(x => x.RowVersion).IsRowVersion();
        b.Entity<DailyCapacity>().Property(x => x.RowVersion).IsRowVersion();
        b.Entity<DailyCapacity>().Ignore(x => x.Remaining);
        b.Entity<BranchFood>().HasIndex(x => new { x.BranchId, x.FoodId }).IsUnique();
        b.Entity<DailyCapacity>().HasIndex(x => new { x.BranchFoodId, x.Date }).IsUnique();
        b.Entity<GroupFood>().HasIndex(x => new { x.BranchGroupId, x.BranchFoodId }).IsUnique();
        b.Entity<AllowedAddon>().HasIndex(x => new { x.ParentBranchFoodId, x.AddonBranchFoodId }).IsUnique();
        b.Entity<StaffBranch>().HasIndex(x => new { x.UserId, x.BranchId }).IsUnique();
        b.Entity<BranchFood>().HasOne(x => x.Food).WithMany().HasForeignKey(x => x.FoodId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Food>().HasMany(x => x.Images).WithOne().HasForeignKey(x => x.FoodId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BranchFood>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BranchGroup>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GroupFood>().HasOne<BranchGroup>().WithMany().HasForeignKey(x => x.BranchGroupId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<GroupFood>().HasOne<BranchFood>().WithMany().HasForeignKey(x => x.BranchFoodId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AllowedAddon>().HasOne<BranchFood>().WithMany().HasForeignKey(x => x.ParentBranchFoodId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AllowedAddon>().HasOne<BranchFood>().WithMany().HasForeignKey(x => x.AddonBranchFoodId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<DailyCapacity>().HasOne<BranchFood>().WithMany().HasForeignKey(x => x.BranchFoodId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BranchHours>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StaffBranch>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StaffBranch>().HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<AuditLog>().HasIndex(x => x.OccurredAt);
        b.Entity<ErrorLog>().HasIndex(x => x.OccurredAt);
        foreach (var property in b.Model.GetEntityTypes().Where(e => !e.Name.Contains("Identity")).SelectMany(e => e.GetProperties()).Where(p => p.ClrType == typeof(string)))
            property.SetMaxLength(property.Name is "Details" or "StackTrace" or "Message" ? 8000 : property.Name == "Description" ? 4000 : 500);
        // این FK باید با کلید nvarchar(450) در Identity برابر باشد؛ قاعده عمومی متن‌ها کافی نیست.
        b.Entity<StaffBranch>().Property(x => x.UserId).HasMaxLength(450);
        b.Entity<BranchSpecial>().HasIndex(x => new { x.BranchId, x.BranchFoodId }).IsUnique();
        b.Entity<BranchSpecial>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BranchSpecial>().HasOne(x => x.BranchFood).WithMany().HasForeignKey(x => x.BranchFoodId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BranchBanner>().HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BranchBanner>().HasOne(x => x.BranchFood).WithMany().HasForeignKey(x => x.BranchFoodId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<BranchBanner>().Property(x => x.Title).HasMaxLength(200);
        b.Entity<BranchBanner>().Property(x => x.Text).HasMaxLength(2000);

    }
}
