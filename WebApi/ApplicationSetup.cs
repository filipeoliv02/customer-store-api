using System;
using CustomerStoreApi.Managers;
using CustomerStoreApi.Services;
using CustomerStoreApi.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CustomerStoreApi;

/// <summary>
/// Provides the service registration and HTTP pipeline configuration of the application.
/// </summary>
public static class ApplicationSetup
{
    /// <summary>
    /// The path of the health check endpoint.
    /// </summary>
    public const string HealthCheckPath = "/health";

    /// <summary>
    /// Registers the services required by the application.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddCustomerStoreApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Persistence (in-memory only, see README).
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("CustomerStoreApiDb"));

        services.AddScoped<ICustomersManager, CustomersManager>();

        services.AddPositionStackGeolocation(configuration);

        services.AddControllers();
        services.AddProblemDetails();
        services.AddHealthChecks();

        services.AddOpenApiDocument(options =>
        {
            options.DocumentName = "v1";
            options.Title = "Customer Store API";
            options.Description = "REST API for the Customer Store Web application.";
        });

        return services;
    }

    /// <summary>
    /// Configures the HTTP request pipeline and endpoints of the application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same web application, for chaining.</returns>
    public static WebApplication UseCustomerStoreApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Return RFC 7807 problem details for unhandled exceptions and empty error responses;
        // no stack traces are exposed outside the Development environment.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUi();
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.MapControllers();
        app.MapHealthChecks(HealthCheckPath);

        return app;
    }

    private static void AddPositionStackGeolocation(this IServiceCollection services, IConfiguration configuration)
    {
        // The API key comes from configuration / environment variables only and is never committed.
        services
            .AddOptions<PositionStackOptions>()
            .Bind(configuration.GetSection(PositionStackOptions.SectionName))
            .PostConfigure(options => options.ApiKey ??= configuration[PositionStackOptions.LegacyApiKeyConfigurationKey])
            .ValidateDataAnnotations()
            .Validate(
                options => options.BaseAddress.IsAbsoluteUri && options.BaseAddress.Scheme == Uri.UriSchemeHttps,
                "PositionStack:BaseAddress must be an absolute HTTPS URI.")
            .ValidateOnStart();

        services.AddMemoryCache();
        services.AddSingleton<GeolocationRetryPolicy>();

        // Typed client: HttpClient instances and handlers are pooled by IHttpClientFactory.
        services.AddHttpClient<PositionStackGeolocationService>((serviceProvider, client) =>
        {
            PositionStackOptions options = serviceProvider.GetRequiredService<IOptions<PositionStackOptions>>().Value;

            // A trailing slash is required so that relative request URIs are appended to the base path.
            string baseAddress = options.BaseAddress.AbsoluteUri;
            client.BaseAddress = new Uri(baseAddress.EndsWith('/') ? baseAddress : baseAddress + "/");
            client.Timeout = options.Timeout;
        });

        services.AddSingleton<IGeoLocationService>(serviceProvider =>
        {
            PositionStackGeolocationService inner = serviceProvider.GetRequiredService<PositionStackGeolocationService>();
            return new CachedGeolocationService(
                inner,
                serviceProvider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                serviceProvider.GetRequiredService<IOptions<PositionStackOptions>>());
        });
    }
}
