using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CustomerStoreApi.Models;
using Microsoft.Extensions.Logging;

namespace CustomerStoreApi.Services
{
    /// <summary>
    /// Defines the implementation of <see cref="IGeoLocationService"/> using PositionStack-API.
    /// </summary>
    /// <seealso cref="IDisposable" />
    public sealed class PositionStackGeolocationService : IGeoLocationService, IDisposable
    {
        #region Fields

        private bool disposed;

        #endregion

        #region Private Properties

        private HttpClient HttpClient
        {
            get;
            set;
        }

        private ILogger Logger
        {
            get;
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionStackGeolocationService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public PositionStackGeolocationService(ILogger<PositionStackGeolocationService> logger)
        {
            this.HttpClient = new HttpClient
            {
                BaseAddress = new Uri("http://api.positionstack.com")
            };
            this.Logger = logger;
        }

        /// <inheritdoc/>
        public async Task<GeolocationData> GetGeolocationAsync(string address, CancellationToken cancellationToken)
        {
            try
            {
                // Fetch the access key from an environment variable
                string accessKey = Environment.GetEnvironmentVariable("PositionStackApiKey");

                if (string.IsNullOrEmpty(accessKey))
                {
                    this.Logger.LogError("The PositionStack API key is not set.");
                    return null;
                }

                string queryParams = $"access_key={accessKey}" +
                                 $"&query={Uri.EscapeDataString(address)}";

                string requestUri = $"/v1/forward?{queryParams}";

                HttpResponseMessage response = await this.HttpClient.GetAsync(requestUri, cancellationToken)
                                                                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    GeolocationData geolocationData = await response.Content.ReadFromJsonAsync<GeolocationData>(cancellationToken: cancellationToken)
                                                                  .ConfigureAwait(false);

                    if (geolocationData != null && geolocationData.Data.Count > 0)
                    {
                        // append address to the geolocation data
                        foreach (GeolocationDto item in geolocationData.Data)
                        {
                            item.Address = address;
                        }

                        return geolocationData;
                    }
                    else
                    {
                        this.Logger.LogError($"The PositionStack API returned an error: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                }
            }
            catch (ArgumentNullException)
            {
                this.Logger.LogError("The PositionStack API key is not set.");
            }
            catch (HttpRequestException ex)
            {
                this.Logger.LogError(ex, "The PositionStack API request failed.");
            }
            catch (TaskCanceledException)
            {
                this.Logger.LogWarning("The PositionStack API request was cancelled.");
            }

            return null;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #region Private Methods

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.HttpClient != null)
                    {
                        this.HttpClient.Dispose();
                    }
                }

                this.disposed = true;
            }
        }

        #endregion
    }
}
