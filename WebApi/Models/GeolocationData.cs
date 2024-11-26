using System.Collections.Generic;

namespace CustomerStoreApi.Models
{
    /// <summary>
    /// JSON data returned by the PositionStack API.
    /// </summary>
    public class GeolocationData
    {
        /// <summary>
        /// Gets or sets a list of geolocation data.
        /// </summary>
        public List<GeolocationDto> Data { get; set; }
    }
}
