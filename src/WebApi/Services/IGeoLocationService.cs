using System.Threading;
using System.Threading.Tasks;
using CustomerStoreApi.Models;

namespace CustomerStoreApi.Services
{
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
        /// The <see cref="Task{TResult}" /> that represents the asynchronous operation.
        /// The <see cref="GeolocationDto" /> that describes the geolocation.
        /// </returns>
        Task<GeolocationData> GetGeolocationAsync(string address, CancellationToken cancellationToken);
    }
}
