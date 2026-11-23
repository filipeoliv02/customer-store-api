using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using CustomerStoreApi.Models;
using CustomerStoreApi.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace CustomerStoreApi.Tests.Services;

/// <summary>
/// Provides tests for normalized in-process geolocation caching.
/// </summary>
public sealed class CachedGeolocationServiceTests
{
    /// <summary>
    /// Ensures equivalent normalized addresses share one cached provider result.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task NormalizesAddressAndCachesResultAsync()
    {
        RecordingGeoLocationService inner = new();
        using MemoryCache cache = new(new MemoryCacheOptions());
        CachedGeolocationService service = new(inner, cache, Options.Create(new PositionStackOptions
        {
            CacheTtl = TimeSpan.FromMinutes(5),
        }));

        GeolocationResponse? first = await service.GetGeolocationAsync("  Rua   Áurea, Porto  ", TestContext.Current.CancellationToken);
        GeolocationResponse? second = await service.GetGeolocationAsync("rua aurea, porto", TestContext.Current.CancellationToken);

        first.Should().BeSameAs(second);
        inner.Calls.Should().Be(1);
    }

    /// <summary>
    /// Ensures normalization removes diacritics and repeated whitespace.
    /// </summary>
    [Fact]
    public void NormalizationIsDiacriticAndWhitespaceInsensitive()
    {
        CachedGeolocationService.NormalizeAddress("  Ávô  ").Should().Be("avo");
        CachedGeolocationService.NormalizeAddress("Rua\t do   Ouro").Should().Be("rua do ouro");
    }

    /// <summary>
    /// Ensures provider misses are not stored.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task DoesNotCacheMissingResultsAsync()
    {
        RecordingGeoLocationService inner = new(includeResult: false);
        using MemoryCache cache = new(new MemoryCacheOptions());
        CachedGeolocationService service = new(inner, cache, Options.Create(new PositionStackOptions()));

        await service.GetGeolocationAsync("Porto", TestContext.Current.CancellationToken);
        await service.GetGeolocationAsync("Porto", TestContext.Current.CancellationToken);

        inner.Calls.Should().Be(2);
    }

    private sealed class RecordingGeoLocationService(bool includeResult = true) : IGeoLocationService
    {
        public int Calls { get; private set; }

        public Task<GeolocationResponse?> GetGeolocationAsync(string address, CancellationToken cancellationToken = default)
        {
            this.Calls++;
            GeolocationResponse? result = includeResult
                ? new GeolocationResponse { Data = [new GeolocationDto { Address = address }] }
                : null;
            return Task.FromResult(result);
        }
    }
}
