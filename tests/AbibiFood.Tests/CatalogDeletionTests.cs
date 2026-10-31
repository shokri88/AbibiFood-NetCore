using AbibiFood.Application.Abstractions;
using AbibiFood.Application.Services;
using AbibiFood.Domain.Entities;
using AbibiFood.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace AbibiFood.Tests;

public sealed class CatalogDeletionTests
{
    private sealed class Scope(bool manager, int branch = 1) : IUserScope
    {
        public string UserId => "delete-test";
        public bool IsManager => manager;
        public Task EnsureBranchAsync(int id, CancellationToken ct) => manager || branch == id ? Task.CompletedTask : throw new UnauthorizedAccessException();
    }
    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task DeletesIndependentFoodAndKeepsAuditId()
    {
        await using var db = Database(); var food = new Food { Name = "غذا", Title = "عنوان" };
        db.Foods.Add(food); await db.SaveChangesAsync(); var id = food.Id;
        await new CatalogService(db, new Scope(true)).DeleteAsync(nameof(Food), id, default);
        Assert.False(await db.Foods.AnyAsync());
        var audit = await db.AuditLogs.SingleAsync();
        Assert.Equal("DeleteFood", audit.Action); Assert.Equal(id.ToString(), audit.EntityId);
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FoodWithImageOrBranchOfferingCannotBeDeleted(bool image)
    {
        await using var db = Database(); db.Foods.Add(new Food { Id = 1 });
        if (image) db.FoodImages.Add(new FoodImage { FoodId = 1 });
        else db.BranchFoods.Add(new BranchFood { FoodId = 1, BranchId = 1 });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => new CatalogService(db, new Scope(true)).DeleteAsync(nameof(Food), 1, default));
        Assert.True(await db.Foods.AnyAsync()); Assert.False(await db.AuditLogs.AnyAsync());
    }
    [Fact]
    public async Task EmployeeCannotDeleteCatalogOrAnotherBranchCapacity()
    {
        await using var db = Database(); db.BranchFoods.Add(new BranchFood { Id = 1, BranchId = 2 });
        db.DailyCapacities.Add(new DailyCapacity { Id = 1, BranchFoodId = 1 }); await db.SaveChangesAsync();
        var service = new CatalogService(db, new Scope(false));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteAsync(nameof(Food), 1, default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteAsync(nameof(DailyCapacity), 1, default));
        Assert.True(await db.DailyCapacities.AnyAsync());
    }
    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    public async Task CapacityWithReservationsOrSalesCannotBeDeleted(int reserved, int sold)
    {
        await using var db = Database(); db.BranchFoods.Add(new BranchFood { Id = 1, BranchId = 1 });
        db.DailyCapacities.Add(new DailyCapacity { Id = 1, BranchFoodId = 1, Reserved = reserved, Sold = sold }); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => new CatalogService(db, new Scope(true)).DeleteAsync(nameof(DailyCapacity), 1, default));
        Assert.True(await db.DailyCapacities.AnyAsync());
    }
    [Fact]
    public async Task EmployeeCanDeleteUnusedCapacityOfAssignedBranch()
    {
        await using var db = Database(); db.BranchFoods.Add(new BranchFood { Id = 1, BranchId = 1 });
        db.DailyCapacities.Add(new DailyCapacity { Id = 1, BranchFoodId = 1 }); await db.SaveChangesAsync();
        await new CatalogService(db, new Scope(false)).DeleteAsync(nameof(DailyCapacity), 1, default);
        Assert.False(await db.DailyCapacities.AnyAsync());
        Assert.Equal(1, (await db.AuditLogs.SingleAsync()).BranchId);
    }
}
