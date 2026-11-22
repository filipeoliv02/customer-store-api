using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CustomerStoreApi.Models;

/// <summary>
/// Defines the geolocation information of a customer's address.
/// </summary>
public sealed class GeolocationResponse
{
    /// <summary>
    /// Gets the collection of geolocation items returned for the address.
    /// </summary>
    [JsonPropertyName("data")]
    public IList<GeolocationDto> Data { get; init; } = [];
}
