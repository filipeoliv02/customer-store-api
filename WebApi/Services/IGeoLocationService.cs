using System.Threading;
using System.Threading.Tasks;
using CustomerStoreApi.Models;

namespace CustomerStoreApi.Services;

/// <summary>
/// Defines the interface of the geolocation service.
/// </summary>
public interface IGeoLocationService
{
    /// <summary>
    /// Gets the geolocation of a specific address.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The geolocation of the address, or <c>null</c> when it could not be obtained.
    /// </returns>
    Task<GeolocationResponse?> GetGeolocationAsync(string address, CancellationToken cancellationToken = default);
}
