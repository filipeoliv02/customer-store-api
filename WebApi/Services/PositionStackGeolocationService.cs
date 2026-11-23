using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CustomerStoreApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerStoreApi.Services;

/// <summary>
/// Defines the implementation of <see cref="IGeoLocationService"/> using the
/// <see href="https://positionstack.com/">PositionStack</see> forward geocoding API.
/// </summary>
/// <remarks>
/// This is a typed HTTP client: the <see cref="HttpClient"/> instance is owned and pooled by
/// <see cref="IHttpClientFactory"/>, so this class must not dispose it.
/// The API key is sent as a query string parameter (as required by PositionStack) and is never logged.
/// </remarks>
public sealed partial class PositionStackGeolocationService : IGeoLocationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly IOptions<PositionStackOptions> options;
    private readonly ILogger<PositionStackGeolocationService> logger;
    private readonly GeolocationRetryPolicy retryPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionStackGeolocationService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client, configured with the PositionStack base address.</param>
    /// <param name="options">The PositionStack options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="retryPolicy">An optional testable retry policy.</param>
    public PositionStackGeolocationService(
        HttpClient httpClient,
        IOptions<PositionStackOptions> options,
        ILogger<PositionStackGeolocationService> logger,
        GeolocationRetryPolicy? retryPolicy = null)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.retryPolicy = retryPolicy ?? new GeolocationRetryPolicy(options);
    }

    /// <inheritdoc/>
    public async Task<GeolocationResponse?> GetGeolocationAsync(string address, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        string? accessKey = this.options.Value.ApiKey;

        if (string.IsNullOrWhiteSpace(accessKey))
        {
            this.LogApiKeyMissing();
            return null;
        }

        Uri requestUri = new(
            $"forward?access_key={Uri.EscapeDataString(accessKey)}&query={Uri.EscapeDataString(address)}",
            UriKind.Relative);

        try
        {
            using HttpResponseMessage response = await this.retryPolicy
                .SendAsync(this.httpClient, requestUri, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                this.LogUnsuccessfulResponse(response.StatusCode);
                return null;
            }

            using JsonDocument document = await JsonDocument
                .ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            List<GeolocationDto>? items = ReadLocations(document.RootElement);

            if (items is null || items.Count == 0)
            {
                this.LogNoResults(TryGetErrorCode(document.RootElement));
                return null;
            }

            foreach (GeolocationDto item in items)
            {
                item.Address = address;
            }

            return new GeolocationResponse { Data = items };
        }
        catch (HttpRequestException ex)
        {
            this.LogRequestFailed(ex);
        }
        catch (JsonException ex)
        {
            this.LogInvalidResponse(ex);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            this.LogTimeout();
        }

        return null;
    }

    /// <summary>
    /// Reads the location items from a PositionStack response.
    /// Supports both <c>{"data": [...]}</c> and <c>{"data": {"results": [...]}}</c> shapes.
    /// </summary>
    private static List<GeolocationDto>? ReadLocations(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("data", out JsonElement data))
        {
            return null;
        }

        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("results", out JsonElement results))
        {
            data = results;
        }

        return data.ValueKind == JsonValueKind.Array
            ? data.Deserialize<List<GeolocationDto>>(SerializerOptions)
            : null;
    }

    private static string? TryGetErrorCode(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty("error", out JsonElement error)
        && error.ValueKind == JsonValueKind.Object
        && error.TryGetProperty("code", out JsonElement code)
            ? code.ToString()
            : null;

    [LoggerMessage(EventId = 100, Level = LogLevel.Error, Message = "The PositionStack API key is not configured. Set the 'PositionStackApiKey' environment variable.")]
    private partial void LogApiKeyMissing();

    [LoggerMessage(EventId = 101, Level = LogLevel.Warning, Message = "The PositionStack API returned status code {StatusCode}.")]
    private partial void LogUnsuccessfulResponse(HttpStatusCode statusCode);

    [LoggerMessage(EventId = 102, Level = LogLevel.Warning, Message = "The PositionStack API returned no results (error code: {ErrorCode}).")]
    private partial void LogNoResults(string? errorCode);

    [LoggerMessage(EventId = 103, Level = LogLevel.Error, Message = "The PositionStack API request failed.")]
    private partial void LogRequestFailed(Exception exception);

    [LoggerMessage(EventId = 104, Level = LogLevel.Error, Message = "The PositionStack API returned an invalid response.")]
    private partial void LogInvalidResponse(Exception exception);

    [LoggerMessage(EventId = 105, Level = LogLevel.Warning, Message = "The PositionStack API request timed out.")]
    private partial void LogTimeout();
}
