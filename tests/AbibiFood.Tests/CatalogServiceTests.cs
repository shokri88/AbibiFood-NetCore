using AbibiFood.Application.Abstractions;
using AbibiFood.Application.Models;
using AbibiFood.Application.Services;
using AbibiFood.Domain.Entities;
using AbibiFood.Domain.Rules;
using AbibiFood.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace AbibiFood.Tests;
public sealed class CatalogServiceTests
{
    private sealed class Scope(bool manager,int branch) : IUserScope
    {
        public string UserId => "test-employee";
        public bool IsManager => manager;
        public Task EnsureBranchAsync(int branchId,CancellationToken ct) => manager||branchId==branch?Task.CompletedTask:throw new UnauthorizedAccessException();
    }
    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    [Fact] public async Task EmployeeCannotChangePrices()
    {
        await using var db=Database();var service=new CatalogService(db,new Scope(false,1));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.SaveBranchFoodAsync(new BranchFoodInput{BranchId=1,Price=100},default));
    }
    [Fact] public async Task EmployeeCannotAllocateAnotherBranchCapacity()
    {
        await using var db=Database();db.BranchFoods.Add(new BranchFood{Id=1,BranchId=2,FoodId=1});await db.SaveChangesAsync();
        var service=new CatalogService(db,new Scope(false,1));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.SaveCapacityAsync(new CapacityInput{BranchFoodId=1,Date=DateOnly.FromDateTime(DateTime.UtcNow),Total=5},default));
    }
    [Fact] public async Task CreationAuditContainsGeneratedId()
    {
        await using var db=Database();var service=new CatalogService(db,new Scope(true,1));
        var id=await service.SaveFoodAsync(new FoodInput{Name="کچاپ",Title="سس کچاپ"},default);
        var audit=await db.AuditLogs.SingleAsync();Assert.Equal(id.ToString(),audit.EntityId);Assert.Equal("test-employee",audit.ActorId);
    }
    [Fact] public async Task FutureCapacityIsDailyAndDuplicateNeedsVersion()
    {
        await using var db=Database();db.BranchFoods.Add(new BranchFood{Id=1,BranchId=1,FoodId=1});await db.SaveChangesAsync();
        var service=new CatalogService(db,new Scope(false,1));var input=new CapacityInput{BranchFoodId=1,Date=DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2),Total=12};
        await service.SaveCapacityAsync(input,default);Assert.Equal(12,(await db.DailyCapacities.SingleAsync()).Remaining);
        await Assert.ThrowsAsync<ConflictException>(()=>service.SaveCapacityAsync(input,default));
    }
}
