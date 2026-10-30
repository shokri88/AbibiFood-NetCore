using System.Security.Cryptography;
using AbibiFood.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace AbibiFood.Tests.Integration;

// این میزبان، مسیر HTTP واقعی را تست می‌کند ولی SQL را با InMemory جایگزین می‌کند.
// آزمون تراکنش و RowVersion واقعی در SqlServerAcceptanceTests جداست.
public sealed class PhaseOneFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string database = "AbibiHttp_" + Guid.NewGuid().ToString("N");
    private readonly Dictionary<string, string?> previous = [];

    public PhaseOneFactory()
    {
        Set("ConnectionStrings__DefaultConnection", "Server=localhost;Database=NotUsedByHttpTests;Integrated Security=True");
        Set("Jwt__Key", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
    }

    private void Set(string name, string value)
    {
        previous[name] = Environment.GetEnvironmentVariable(name);
        Environment.SetEnvironmentVariable(name, value);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(database));
        });
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        foreach (var (name, value) in previous) Environment.SetEnvironmentVariable(name, value);
    }
}

[CollectionDefinition("PhaseOneHttp", DisableParallelization = true)]
public sealed class PhaseOneHttpCollection : ICollectionFixture<PhaseOneFactory>;
