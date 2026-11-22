using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using CustomerStoreApi.Models;
using CustomerStoreApi.Services;
using CustomerStoreApi.Tests.Fakes;
using Microsoft.Extensions.Options;
using Xunit;

namespace CustomerStoreApi.Tests.Services;

/// <summary>
/// Provides unit tests for the <see cref="PositionStackGeolocationService"/> type, using a stubbed HTTP handler.
/// </summary>
public sealed class PositionStackGeolocationServiceTests : IDisposable
{
    // Dummy value used only in tests; not a real credential.
    private const string ApiKey = "dummy-test-key";

    private const string Address = "Rua do Ouro, Porto, Portugal";

    private const string SuccessJson = """
        {
          "data": [
            { "latitude": 41.14, "longitude": -8.64, "type": "street", "locality": "Porto", "region": "Porto", "country": "Portugal", "label": "Rua do Ouro" },
            { "latitude": 38.71, "longitude": -9.14, "type": "street", "locality": "Lisboa", "region": "Lisboa", "country": "Portugal" }
          ]
        }
        """;

    private readonly ListLogger<PositionStackGeolocationService> logger = new();
    private readonly List<HttpClient> httpClients = [];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (HttpClient httpClient in this.httpClients)
        {
            httpClient.Dispose();
        }
    }

    /// <summary>
    /// Ensures that the request is sent over HTTPS with the key and escaped address, and that the results are mapped.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ReturnsLocationsAsync()
    {
        using StubHttpMessageHandler handler = new(_ => Json(HttpStatusCode.OK, SuccessJson));

        GeolocationResponse? result = await this.CreateService(handler).GetGeolocationAsync(Address, CancellationToken);

        result.Should().NotBeNull();
        result!.Data.Should().HaveCount(2);
        result.Data[0].Should().BeEquivalentTo(new GeolocationDto
        {
            Address = Address,
            Latitude = 41.14,
            Longitude = -8.64,
            Type = "street",
            Locality = "Porto",
            Region = "Porto",
            Country = "Portugal",
        });
        result.Data.Should().OnlyContain(item => item.Address == Address);

        Uri requestUri = handler.RequestUris.Should().ContainSingle().Which!;
        requestUri.Scheme.Should().Be(Uri.UriSchemeHttps);
        requestUri.GetLeftPart(UriPartial.Path).Should().Be("https://api.positionstack.com/v1/forward");
        requestUri.Query.Should().Be($"?access_key={ApiKey}&query={Uri.EscapeDataString(Address)}");
    }

    /// <summary>
    /// Ensures that the <c>{"data": {"results": [...]}}</c> response shape is supported.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task SupportsNestedResultsShapeAsync()
    {
        using StubHttpMessageHandler handler = new(_ => Json(HttpStatusCode.OK, """{ "data": { "results": [ { "latitude": 1.5, "longitude": 2.5 } ] } }"""));

        GeolocationResponse? result = await this.CreateService(handler).GetGeolocationAsync(Address, CancellationToken);

        result!.Data.Should().ContainSingle().Which.Latitude.Should().Be(1.5);
    }

    /// <summary>
    /// Ensures that no request is sent when the API key is not configured.
    /// </summary>
    /// <param name="apiKey">The configured API key.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReturnsNullWithoutApiKeyAsync(string? apiKey)
    {
        using StubHttpMessageHandler handler = new(_ => Json(HttpStatusCode.OK, SuccessJson));

        GeolocationResponse? result = await this.CreateService(handler, apiKey).GetGeolocationAsync(Address, CancellationToken);

        result.Should().BeNull();
        handler.RequestUris.Should().BeEmpty();
    }

    /// <summary>
    /// Ensures that unusable responses produce <c>null</c> and never leak the API key into the logs.
    /// </summary>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="body">The response body.</param>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, """{ "error": { "code": "invalid_access_key" } }""")]
    [InlineData(HttpStatusCode.InternalServerError, "")]
    [InlineData(HttpStatusCode.OK, """{ "error": { "code": "usage_limit_reached", "message": "limit" } }""")]
    [InlineData(HttpStatusCode.OK, """{ "data": [] }""")]
    [InlineData(HttpStatusCode.OK, """{ "data": null }""")]
    [InlineData(HttpStatusCode.OK, "not json")]
    [InlineData(HttpStatusCode.OK, """{ "data": [ { "latitude": "not a number" } ] }""")]
    public async Task ReturnsNullForUnusableResponsesAsync(HttpStatusCode statusCode, string body)
    {
        using StubHttpMessageHandler handler = new(_ => Json(statusCode, body));

        GeolocationResponse? result = await this.CreateService(handler).GetGeolocationAsync(Address, CancellationToken);

        result.Should().BeNull();
        this.logger.Messages.Should().NotBeEmpty();
        this.logger.Messages.Should().NotContain(message => message.Contains(ApiKey, StringComparison.Ordinal));
    }

    /// <summary>
    /// Ensures that transport failures produce <c>null</c>.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ReturnsNullWhenRequestFailsAsync()
    {
        using StubHttpMessageHandler handler = new(_ => throw new HttpRequestException("connection refused"));

        GeolocationResponse? result = await this.CreateService(handler).GetGeolocationAsync(Address, CancellationToken);

        result.Should().BeNull();
    }

    /// <summary>
    /// Ensures that a timeout produces <c>null</c>.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ReturnsNullOnTimeoutAsync()
    {
        using StubHttpMessageHandler handler = new(_ => throw new TaskCanceledException("timeout"));

        GeolocationResponse? result = await this.CreateService(handler).GetGeolocationAsync(Address, CancellationToken);

        result.Should().BeNull();
    }

    /// <summary>
    /// Ensures that cancellation requested by the caller is propagated.
    /// </summary>
    /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
    [Fact]
    public async Task PropagatesCallerCancellationAsync()
    {
        using StubHttpMessageHandler handler = new(_ => Json(HttpStatusCode.OK, SuccessJson));
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        Func<Task<GeolocationResponse?>> act = () => this.CreateService(handler).GetGeolocationAsync(Address, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body) => new(statusCode)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };

    private PositionStackGeolocationService CreateService(StubHttpMessageHandler handler, string? apiKey = ApiKey)
    {
        HttpClient httpClient = new(handler, disposeHandler: false);
        this.httpClients.Add(httpClient);
        httpClient.BaseAddress = new Uri("https://api.positionstack.com/v1/");

        return new PositionStackGeolocationService(
            httpClient,
            Options.Create(new PositionStackOptions { ApiKey = apiKey }),
            this.logger);
    }
}
