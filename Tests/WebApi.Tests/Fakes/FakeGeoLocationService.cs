using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CustomerStoreApi.Models;
using CustomerStoreApi.Services;

namespace CustomerStoreApi.Tests.Fakes;

/// <summary>
/// Defines a fake <see cref="IGeoLocationService"/> that returns a configurable response.
/// </summary>
public sealed class FakeGeoLocationService : IGeoLocationService
{
    /// <summary>
    /// Gets or sets the response returned by <see cref="GetGeolocationAsync"/>.
    /// </summary>
    public GeolocationResponse? Response { get; set; }

    /// <summary>
    /// Gets the addresses that were requested.
    /// </summary>
    public ConcurrentQueue<string> RequestedAddresses { get; } = new();

    /// <inheritdoc />
    public Task<GeolocationResponse?> GetGeolocationAsync(string address, CancellationToken cancellationToken = default)
    {
        this.RequestedAddresses.Enqueue(address);
        return Task.FromResult(this.Response);
    }

    /// <summary>
    /// Resets the fake to its initial state.
    /// </summary>
    public void Reset()
    {
        this.Response = null;
        this.RequestedAddresses.Clear();
    }
}
