using System.Text.Json.Serialization;

namespace RocketStoreApi.Models
{
    /// <summary>
    /// Defines the geolocation dto.
    /// </summary>
    public class GeolocationDto
    {
        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
        
        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the locality.
        /// </summary>
        [JsonPropertyName("locality")]
        public string Locality { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        [JsonPropertyName("region")]
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the country.
        /// </summary>
        [JsonPropertyName("country")]
        public string Country { get; set; }
    }
}
