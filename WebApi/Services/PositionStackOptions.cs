using System;
using System.ComponentModel.DataAnnotations;

namespace CustomerStoreApi.Services;

/// <summary>
/// Defines the configuration of the PositionStack geolocation service.
/// </summary>
/// <remarks>
/// Bound from the <c>PositionStack</c> configuration section. The API key can be supplied either as
/// <c>PositionStack__ApiKey</c> or, for backwards compatibility, as the <c>PositionStackApiKey</c>
/// environment variable. It must never be committed to source control.
/// </remarks>
public sealed class PositionStackOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "PositionStack";

    /// <summary>
    /// The legacy configuration key (environment variable) that holds the API key.
    /// </summary>
    public const string LegacyApiKeyConfigurationKey = "PositionStackApiKey";

    /// <summary>
    /// Gets or sets the base address of the PositionStack API.
    /// </summary>
    [Required]
    public Uri BaseAddress { get; set; } = new("https://api.positionstack.com/v1/");

    /// <summary>
    /// Gets or sets the PositionStack API access key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the HTTP request timeout.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets how long normalized geolocation results are retained in memory.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the maximum number of HTTP retry attempts after the initial attempt.
    /// </summary>
    [Range(0, 5)]
    public int RetryCount { get; set; } = 2;

    /// <summary>
    /// Gets or sets the initial exponential backoff delay.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:00", "00:00:10")]
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);
}
