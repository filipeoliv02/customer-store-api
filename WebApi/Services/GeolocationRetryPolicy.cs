using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerStoreApi.Services;

/// <summary>
/// Provides a small, testable retry policy for transient PositionStack HTTP failures.
/// </summary>
public sealed class GeolocationRetryPolicy
{
    private readonly PositionStackOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeolocationRetryPolicy"/> class.
    /// </summary>
    /// <param name="options">The PositionStack resilience options.</param>
    public GeolocationRetryPolicy(Microsoft.Extensions.Options.IOptions<PositionStackOptions> options)
    {
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GeolocationRetryPolicy"/> class.
    /// </summary>
    /// <param name="options">The resilience options.</param>
    /// <param name="jitterProvider">The fixed or generated jitter provider.</param>
    internal GeolocationRetryPolicy(PositionStackOptions options, Func<double> jitterProvider)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.JitterProvider = jitterProvider ?? throw new ArgumentNullException(nameof(jitterProvider));
    }

    private Func<double>? JitterProvider { get; }

    /// <summary>
    /// Sends an HTTP request, retrying transient failures with jittered exponential backoff.
    /// </summary>
    /// <param name="httpClient">The configured HTTP client.</param>
    /// <param name="requestUri">The relative or absolute request URI.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The final HTTP response.</returns>
    public async Task<HttpResponseMessage> SendAsync(
        HttpClient httpClient,
        Uri requestUri,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        for (int attempt = 0; ; attempt++)
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);

                if (!IsTransient(response.StatusCode) || attempt >= this.options.RetryCount)
                {
                    return response;
                }

                response.Dispose();
            }
            catch (HttpRequestException) when (attempt < this.options.RetryCount)
            {
                // Retry transport failures unless the caller cancelled the operation.
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < this.options.RetryCount)
            {
                // HttpClient timeout is treated as transient.
            }

            TimeSpan delay = TimeSpan.FromTicks((long)(this.options.RetryBaseDelay.Ticks * Math.Pow(2, attempt)));
            double jitter = this.JitterProvider?.Invoke() ?? RandomNumberGenerator.GetInt32(0, 1_000_000) / 1_000_000d;
            delay += TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 0.25 * jitter);
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout
        || statusCode == HttpStatusCode.TooManyRequests
        || (int)statusCode >= 500;
}
