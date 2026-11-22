using CustomerStoreApi;
using Microsoft.AspNetCore.Builder;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomerStoreApi(builder.Configuration);

WebApplication app = builder.Build();

app.UseCustomerStoreApi();

await app.RunAsync().ConfigureAwait(false);

/// <summary>
/// Defines the entry point of the application. Declared as partial so that it can be used by integration tests.
/// </summary>
public partial class Program
{
}
