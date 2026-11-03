using AbibiFood.Application.Abstractions;
using AbibiFood.Application.Models;
using AbibiFood.Application.Services;
using AbibiFood.Domain.Entities;
using AbibiFood.Domain.Rules;
using AbibiFood.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace AbibiFood.Tests;

public sealed class RecurringCapacityTests
{
    private sealed class Employee : IUserScope
    {
        public string UserId => "capacity-employee";
        public bool IsManager => false;
        public Task EnsureBranchAsync(int branchId, CancellationToken ct) => branchId == 1 ? Task.CompletedTask : throw new UnauthorizedAccessException();
    }
    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static CapacityDefaultsInput Input(params int[] ids) => new()
    {
        BranchId = 1, Total = 30, WarningThreshold = 3,
        Foods = ids.Select(id => new CapacitySelection { BranchFoodId = id, Selected = true, Version = "" }).ToList()
    };
    [Fact]
    public void TehranMidnightStartsNewDayWithoutCarryingConsumption()
    {
        var before = CapacitySchedule.Today(new DateTimeOffset(2026, 11, 3, 20, 29, 59, TimeSpan.Zero));
        var after = CapacitySchedule.Today(new DateTimeOffset(2026, 11, 3, 20, 30, 0, TimeSpan.Zero));
        Assert.Equal(before.AddDays(1), after);
        var food = new BranchFood { Id = 1, DefaultDailyCapacity = 30 };
        var recorded = new DailyCapacity { BranchFoodId = 1, Date = before, Total = 30, Reserved = 4, Sold = 26 };
        Assert.Equal(0, CapacitySchedule.Resolve(food, before, before, recorded)!.Remaining);
        Assert.Equal(30, CapacitySchedule.Resolve(food, after, after, null)!.Remaining);
        Assert.Equal(26, recorded.Sold);
        Assert.Null(CapacitySchedule.Resolve(food, before, after, null));
    }
    [Fact]
    public async Task BulkDefaultsAreAuditedAndDoNotOverwriteDatedConsumption()
    {
        await using var db = Database();
        db.BranchFoods.AddRange(new BranchFood { Id = 1, BranchId = 1 }, new BranchFood { Id = 2, BranchId = 1 });
        var today = CapacitySchedule.Today();
        db.DailyCapacities.Add(new DailyCapacity { BranchFoodId = 1, Date = today, Total = 12, Sold = 7, Reserved = 2 });
        await db.SaveChangesAsync();
        await new CatalogService(db, new Employee()).SaveCapacityDefaultsAsync(Input(1, 2), default);
        Assert.All(await db.BranchFoods.ToListAsync(), f => Assert.Equal(30, f.DefaultDailyCapacity));
        var current = await CapacityReader.ReadAsync(db, 1, today, today, default);
        Assert.Equal(3, current.Single(c => c.BranchFoodId == 1).Remaining);
        Assert.Equal(30, current.Single(c => c.BranchFoodId == 2).Remaining);
        var next = await CapacityReader.ReadAsync(db, 1, today.AddDays(1), today.AddDays(1), default);
        Assert.All(next, c => Assert.Equal(30, c.Remaining));
        Assert.Single(await db.DailyCapacities.ToListAsync());
        Assert.Equal("SaveCapacityDefaults", (await db.AuditLogs.SingleAsync()).Action);
    }
    [Fact]
    public async Task MixedBranchesOrStaleVersionsCannotPartiallyUpdateSelection()
    {
        await using var db = Database();
        db.BranchFoods.AddRange(new BranchFood { Id = 1, BranchId = 1 }, new BranchFood { Id = 2, BranchId = 2 });
        await db.SaveChangesAsync();
        var service = new CatalogService(db, new Employee());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveCapacityDefaultsAsync(Input(1, 2), default));
        Assert.All(await db.BranchFoods.ToListAsync(), f => Assert.Null(f.DefaultDailyCapacity));
        var stale = Input(1); stale.Foods[0].Version = "AQ==";
        await Assert.ThrowsAsync<ConflictException>(() => service.SaveCapacityDefaultsAsync(stale, default));
        var forbidden = Input(2); forbidden.BranchId = 2;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SaveCapacityDefaultsAsync(forbidden, default));
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }
    [Fact]
    public async Task EmptyDuplicateNegativeAndDisabledDefaultsAreHandled()
    {
        await using var db = Database(); db.BranchFoods.Add(new BranchFood { Id = 1, BranchId = 1 }); await db.SaveChangesAsync();
        var service = new CatalogService(db, new Employee());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveCapacityDefaultsAsync(Input(), default));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveCapacityDefaultsAsync(Input(1, 1), default));
        var negative = Input(1); negative.Total = -1;
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveCapacityDefaultsAsync(negative, default));
        var disabled = Input(1); disabled.IsActive = false;
        await service.SaveCapacityDefaultsAsync(disabled, default);
        var capacity = Assert.Single(await CapacityReader.ReadAsync(db, 1, CapacitySchedule.Today(), CapacitySchedule.Today(), default));
        Assert.False(capacity.IsActive);
        Assert.Null(CapacitySchedule.Resolve(new BranchFood(), CapacitySchedule.Today(), CapacitySchedule.Today(), null));
    }
}
