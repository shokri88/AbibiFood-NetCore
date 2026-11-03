using AbibiFood.Application.Abstractions;
using AbibiFood.Application.Models;
using AbibiFood.Application.Services;
using AbibiFood.Domain.Entities;
using AbibiFood.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace AbibiFood.Tests;
public sealed class HomeContentTests
{
    private sealed class Scope(bool manager) : IUserScope
    {
        public string UserId => "home-content-manager";
        public bool IsManager => manager;
        public Task EnsureBranchAsync(int id, CancellationToken ct) => manager || id == 1 ? Task.CompletedTask : throw new UnauthorizedAccessException();
    }
    private static async Task<AppDbContext> Database()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.BranchFoods.AddRange(new BranchFood { Id = 1, BranchId = 1 }, new BranchFood { Id = 2, BranchId = 2 });
        await db.SaveChangesAsync(); return db;
    }
    [Fact]
    public async Task SpecialsAreScopedUniqueOrderedAndAudited()
    {
        await using var db = await Database(); var service = new CatalogService(db, new Scope(true));
        var id = await service.SaveSpecialAsync(new SpecialInput { BranchId = 1, BranchFoodId = 1, SortOrder = 4 }, default);
        Assert.Equal(4, (await db.BranchSpecials.SingleAsync()).SortOrder);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveSpecialAsync(new SpecialInput { BranchId = 1, BranchFoodId = 1 }, default));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveSpecialAsync(new SpecialInput { BranchId = 1, BranchFoodId = 2 }, default));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveSpecialAsync(new SpecialInput { Id = id, BranchId = 2, BranchFoodId = 2 }, default));
        await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteAsync(nameof(BranchFood), 1, default));
        Assert.Equal("SaveSpecial", (await db.AuditLogs.SingleAsync()).Action);
        await service.DeleteAsync(nameof(BranchSpecial), id, default);
        Assert.Empty(await db.BranchSpecials.ToListAsync());
    }
    [Fact]
    public async Task BannersHaveIndependentContentAndProtectExistingBranch()
    {
        await using var db = await Database(); var service = new CatalogService(db, new Scope(true));
        var input = new BannerInput { BranchId = 1, BranchFoodId = 1, Title = "عنوان کاربر", Text = "متن کاربر", ImagePath = "/uploads/catalog/banner.png", SortOrder = 3 };
        var id = await service.SaveBannerAsync(input, default);
        var banner = await db.BranchBanners.SingleAsync();
        Assert.Equal(input.Title, banner.Title); Assert.Equal(input.Text, banner.Text); Assert.Equal(input.ImagePath, banner.ImagePath);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveBannerAsync(new BannerInput { Id = id, BranchId = 2, BranchFoodId = 2, Title = "عنوان", Text = "متن", ImagePath = input.ImagePath }, default));
        await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteAsync(nameof(BranchFood), 1, default));
        await service.DeleteAsync(nameof(BranchBanner), id, default);
        Assert.Empty(await db.BranchBanners.ToListAsync());
    }
    [Fact]
    public async Task EmployeesAndInvalidBannerContentAreRejected()
    {
        await using var db = await Database(); var employee = new CatalogService(db, new Scope(false));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => employee.SaveSpecialAsync(new SpecialInput { BranchId = 1, BranchFoodId = 1 }, default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => employee.SaveBannerAsync(new BannerInput { BranchId = 1, BranchFoodId = 1 }, default));
        var service = new CatalogService(db, new Scope(true));
        foreach (var path in new[] { "", "https://elsewhere.test/image.png", "/uploads/../escape.png" })
            await Assert.ThrowsAsync<ArgumentException>(() => service.SaveBannerAsync(new BannerInput { BranchId = 1, BranchFoodId = 1, Title = "عنوان", Text = "متن", ImagePath = path }, default));
        Assert.Empty(await db.BranchBanners.ToListAsync()); Assert.Empty(await db.AuditLogs.ToListAsync());
    }
}
