using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AwesomeAssertions;
using CustomerStoreApi.Services;
using CustomerStoreApi.Tests.Fakes;
using Xunit;

namespace CustomerStoreApi.Tests.Services;

/// <summary>
/// Provides tests for the PositionStack retry policy.
/// </summary>
public sealed class GeolocationRetryPolicyTests
{
    /// <summary>
    /// Ensures transient responses are retried until success.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RetriesTransientResponsesWithExponentialAttemptsAsync()
    {
        int attempts = 0;
        using StubHttpMessageHandler handler = new(_ => ++attempts switch
        {
            1 => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            2 => new HttpResponseMessage(HttpStatusCode.RequestTimeout),
            _ => new HttpResponseMessage(HttpStatusCode.OK),
        });
        using HttpClient client = new(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://api.positionstack.com/v1/"),
        };
        PositionStackOptions options = new()
        {
            RetryCount = 2,
            RetryBaseDelay = TimeSpan.Zero,
        };
        GeolocationRetryPolicy policy = new(options, () => 0d);

        using HttpResponseMessage response = await policy.SendAsync(
            client,
            new Uri("forward", UriKind.Relative),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        attempts.Should().Be(3);
    }

    /// <summary>
    /// Ensures a non-transient status is returned without retry.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task StopsWithoutRetryForNonTransientStatusAsync()
    {
        int attempts = 0;
        using StubHttpMessageHandler handler = new(_ =>
        {
            attempts++;
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        });
        using HttpClient client = new(handler, disposeHandler: false);
        PositionStackOptions options = new()
        {
            RetryCount = 3,
            RetryBaseDelay = TimeSpan.Zero,
        };
        GeolocationRetryPolicy policy = new(options, () => 0d);

        using HttpResponseMessage response = await policy.SendAsync(
            client,
            new Uri("https://api.positionstack.com/v1/forward"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        attempts.Should().Be(1);
    }

    /// <summary>
    /// Ensures transport failures are retried.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task RetriesTransportFailureAsync()
    {
        int attempts = 0;
        using StubHttpMessageHandler handler = new(_ => ++attempts == 1
            ? throw new HttpRequestException("temporary")
            : new HttpResponseMessage(HttpStatusCode.OK));
        using HttpClient client = new(handler, disposeHandler: false);
        PositionStackOptions options = new()
        {
            RetryCount = 1,
            RetryBaseDelay = TimeSpan.Zero,
        };
        GeolocationRetryPolicy policy = new(options, () => 0d);

        using HttpResponseMessage response = await policy.SendAsync(
            client,
            new Uri("https://api.positionstack.com/v1/forward"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        attempts.Should().Be(2);
    }
}
