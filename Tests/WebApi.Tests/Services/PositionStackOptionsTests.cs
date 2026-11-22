using System;
using System.Collections.Generic;
using AwesomeAssertions;
using CustomerStoreApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CustomerStoreApi.Tests.Services;

/// <summary>
/// Provides tests for the binding and validation of <see cref="PositionStackOptions"/>.
/// </summary>
public sealed class PositionStackOptionsTests
{
    /// <summary>
    /// Ensures that the legacy <c>PositionStackApiKey</c> setting is used when the section key is not set.
    /// </summary>
    [Fact]
    public void UsesLegacyApiKeySetting()
    {
        PositionStackOptions options = Resolve(new() { ["PositionStackApiKey"] = "legacy" });

        options.ApiKey.Should().Be("legacy");
        options.BaseAddress.Should().Be(new Uri("https://api.positionstack.com/v1/"));
    }

    /// <summary>
    /// Ensures that the section key takes precedence over the legacy setting.
    /// </summary>
    [Fact]
    public void PrefersSectionApiKey()
    {
        PositionStackOptions options = Resolve(new() { ["PositionStackApiKey"] = "legacy", ["PositionStack:ApiKey"] = "section" });

        options.ApiKey.Should().Be("section");
    }

    /// <summary>
    /// Ensures that a non-HTTPS base address is rejected.
    /// </summary>
    [Fact]
    public void RejectsInsecureBaseAddress()
    {
        Action act = () => Resolve(new() { ["PositionStack:BaseAddress"] = "http://api.positionstack.com/v1/" });

        act.Should().Throw<OptionsValidationException>();
    }

    private static PositionStackOptions Resolve(Dictionary<string, string?> settings)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddCustomerStoreApi(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<PositionStackOptions>>().Value;
    }
}
