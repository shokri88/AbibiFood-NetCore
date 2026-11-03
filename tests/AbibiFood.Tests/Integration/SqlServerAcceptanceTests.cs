using AbibiFood.Application.Abstractions;
using AbibiFood.Application.Models;
using AbibiFood.Application.Services;
using AbibiFood.Domain.Entities;
using AbibiFood.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;
namespace AbibiFood.Tests.Integration;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        // فقدان محیط واقعی نباید به موفقیت کاذب تست SQL تبدیل شود.
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ABIBI_TEST_SQL")) &&
            Environment.GetEnvironmentVariable("ABIBI_REQUIRE_SQL") != "true")
            Skip = "SQL Server آزمایشی تنظیم نشده؛ ABIBI_TEST_SQL لازم است.";
    }
}

[Collection("PhaseOneHttp")]
public sealed class SqlServerAcceptanceTests(ITestOutputHelper output)
{
    private sealed class Manager(string actorId = "integration-manager") : IUserScope
    {
        public string UserId => actorId;
        public bool IsManager => true;
        public Task EnsureBranchAsync(int branchId, CancellationToken ct) => Task.CompletedTask;
    }

    [SqlServerFact]
    public async Task Migration_SharedDailyCapacity_Concurrency_UniqueIndex_AndAuditRollback()
    {
        var supplied = Environment.GetEnvironmentVariable("ABIBI_TEST_SQL");
        Assert.False(string.IsNullOrWhiteSpace(supplied));
        var connection = new SqlConnectionStringBuilder(supplied!)
        {
            InitialCatalog = "AbibiAcceptance_" + Guid.NewGuid().ToString("N")
        };
        var databaseName = connection.InitialCatalog;
        AppDbContext Create() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection.ConnectionString).Options);
        var migrated = false;
        try
        {
            await using var db = Create();
            await db.Database.MigrateAsync(); migrated = true;
            Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
            await db.Database.OpenConnectionAsync();
            using (var command = db.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(128))";
                output.WriteLine("SQL Server ProductVersion: " + await command.ExecuteScalarAsync());
            }
            await db.Database.CloseConnectionAsync();

            var branch = new Branch { Name = "شعبه تست SQL", Address = "آدرس آزمایشی" };
            var pizza = new Food { Name = "پیتزا", Title = "پیتزا تست" };
            var sauce = new Food { Name = "کچاپ", Title = "کچاپ تست" };
            db.AddRange(branch, pizza, sauce); await db.SaveChangesAsync();
            var main = new BranchFood { BranchId = branch.Id, FoodId = pizza.Id, Price = 100 };
            var extra = new BranchFood { BranchId = branch.Id, FoodId = sauce.Id, Price = 20, CanSellStandalone = true };
            db.AddRange(main, extra); await db.SaveChangesAsync();
            var service = new CatalogService(db, new Manager());
            await service.SaveCapacityDefaultsAsync(new CapacityDefaultsInput
            {
                BranchId = branch.Id, Total = 20,
                Foods = new[] { main, extra }.Select(f => new CapacitySelection { BranchFoodId = f.Id, Selected = true, Version = Convert.ToBase64String(f.RowVersion) }).ToList()
            }, default);
            var recurringDay = AbibiFood.Domain.Rules.CapacitySchedule.Today().AddDays(1);
            Assert.All(await CapacityReader.ReadAsync(db, branch.Id, recurringDay, recurringDay, default), c => Assert.Equal(20, c.Remaining));

            await service.SaveAddonAsync(new AddonInput { ParentBranchFoodId = main.Id, AddonBranchFoodId = extra.Id }, default);
            var day = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2);
            await service.SaveCapacityAsync(new CapacityInput { BranchFoodId = extra.Id, Date = day, Total = 10 }, default);
            Assert.Equal(1, await db.DailyCapacities.CountAsync(x => x.BranchFoodId == extra.Id && x.Date == day));
            Assert.Equal(extra.Id, (await db.AllowedAddons.SingleAsync()).AddonBranchFoodId);
            Assert.True(extra.CanSellStandalone); // همان قلم، هم افزودنی و هم مستقل است.

            await using var first = Create(); await using var second = Create();
            var c1 = await first.DailyCapacities.SingleAsync();
            var c2 = await second.DailyCapacities.SingleAsync();
            var oldVersion = Convert.ToBase64String(c1.RowVersion);
            await new CatalogService(first, new Manager()).SaveCapacityAsync(new CapacityInput
            {
                BranchFoodId = extra.Id,
                Date = day,
                Total = 12,
                Version = oldVersion
            }, default);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => new CatalogService(second, new Manager()).SaveCapacityAsync(new CapacityInput
            {
                BranchFoodId = extra.Id,
                Date = day,
                Total = 11,
                Version = Convert.ToBase64String(c2.RowVersion)
            }, default));
            await using (var verify = Create()) Assert.Equal(12, (await verify.DailyCapacities.SingleAsync()).Total);

            await using (var duplicate = Create())
            {
                duplicate.BranchFoods.Add(new BranchFood { BranchId = branch.Id, FoodId = sauce.Id, Price = 25 });
                await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
            }

            // شکست عمدی Audit در مرحله دوم باید INSERT غذا را هم در SQL rollback کند.
            var marker = "rollback_" + Guid.NewGuid().ToString("N");
            await using (var failing = Create())
            {
                var invalidActor = new Manager(new string('x', 501));
                await Assert.ThrowsAsync<DbUpdateException>(() => new CatalogService(failing, invalidActor)
                    .SaveFoodAsync(new FoodInput { Name = marker, Title = marker }, default));
            }
            await using (var verify = Create()) Assert.False(await verify.Foods.AnyAsync(x => x.Name == marker));
            // شکست Audit نباید پیش‌فرض بخشی از انتخاب گروهی را ذخیره کند.
            await using (var failingDefaults = Create())
            {
                var selectedFoods = await failingDefaults.BranchFoods.ToListAsync();
                await Assert.ThrowsAsync<DbUpdateException>(() => new CatalogService(failingDefaults, new Manager(new string('x', 501)))
                    .SaveCapacityDefaultsAsync(new CapacityDefaultsInput
                    {
                        BranchId = branch.Id, Total = 999,
                        Foods = selectedFoods.Select(f => new CapacitySelection { BranchFoodId = f.Id, Selected = true, Version = Convert.ToBase64String(f.RowVersion) }).ToList()
                    }, default));
            }
            await using (var verify = Create())
                Assert.All(await verify.BranchFoods.ToListAsync(), f => Assert.Equal(20, f.DefaultDailyCapacity));
            output.WriteLine("PASS: migration, shared item capacity, SQL rowversion, unique index and audit rollback.");
        }
        finally
        {
            // هیچ دیتابیس موجودی حذف نمی‌شود؛ فقط نام تصادفی ساخته‌شده همین آزمون.
            if (migrated && databaseName.StartsWith("AbibiAcceptance_", StringComparison.Ordinal))
            {
                await using var cleanup = Create(); await cleanup.Database.EnsureDeletedAsync();
            }
        }
    }
}
