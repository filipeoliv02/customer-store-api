using System;
using System.Threading.Tasks;
using CustomerStoreApi.Services;
using CustomerStoreApi.Storage;
using CustomerStoreApi.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace CustomerStoreApi.Tests;

/// <summary>
/// Defines the web application factory used by the integration tests.
/// Each factory instance uses its own in-memory database and a fake geolocation service,
/// so tests never call the real PositionStack API.
/// </summary>
public sealed class CustomerStoreApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"CustomerStoreApiTests-{Guid.NewGuid()}";

    /// <summary>
    /// Gets the fake geolocation service used by the application.
    /// </summary>
    public FakeGeoLocationService GeoLocationService { get; } = new();

    /// <summary>
    /// Deletes all data and resets the fakes.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    public async Task ResetAsync()
    {
        this.GeoLocationService.Reset();

        AsyncServiceScope scope = this.Services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureDeletedAsync().ConfigureAwait(false);
            await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(this.databaseName));

            services.RemoveAll<IGeoLocationService>();
            services.AddSingleton<IGeoLocationService>(this.GeoLocationService);
        });
    }
}
