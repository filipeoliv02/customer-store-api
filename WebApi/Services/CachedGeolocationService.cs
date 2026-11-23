using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CustomerStoreApi.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CustomerStoreApi.Services;

/// <summary>
/// Adds an in-process, TTL-based cache around a geolocation provider.
/// </summary>
public sealed class CachedGeolocationService : IGeoLocationService
{
    private readonly IGeoLocationService inner;
    private readonly IMemoryCache cache;
    private readonly PositionStackOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedGeolocationService"/> class.
    /// </summary>
    /// <param name="inner">The provider to cache.</param>
    /// <param name="cache">The in-process memory cache.</param>
    /// <param name="options">The cache TTL and provider options.</param>
    public CachedGeolocationService(
        IGeoLocationService inner,
        IMemoryCache cache,
        IOptions<PositionStackOptions> options)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public async Task<GeolocationResponse?> GetGeolocationAsync(string address, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        string key = NormalizeAddress(address);
        if (this.cache.TryGetValue(key, out GeolocationResponse? cached))
        {
            return cached;
        }

        GeolocationResponse? result = await this.inner.GetGeolocationAsync(address, cancellationToken).ConfigureAwait(false);
        if (result is not null)
        {
            this.cache.Set(key, result, this.options.CacheTtl);
        }

        return result;
    }

    /// <summary>
    /// Normalizes an address into a stable cache key.
    /// </summary>
    /// <param name="address">The address to normalize.</param>
    /// <returns>A trim, whitespace, diacritic and case normalized address key.</returns>
    internal static string NormalizeAddress(string address)
    {
        string decomposed = address.Trim().Normalize(NormalizationForm.FormD);
        StringBuilder builder = new(decomposed.Length);

        foreach (char character in decomposed)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark
                && category != UnicodeCategory.SpacingCombiningMark
                && category != UnicodeCategory.EnclosingMark)
            {
                builder.Append(char.IsWhiteSpace(character) ? ' ' : char.ToLowerInvariant(character));
            }
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
