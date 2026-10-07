using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace CaramelCloud.Infrastructure.DTOs.Geocoding
{
    public class NominatimResultDto
    {
        [JsonPropertyName("place_id")]
        public long PlaceId { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("lat")]
        public string Lat { get; set; } = string.Empty;

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = string.Empty;

        public double Latitude => double.TryParse(Lat,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;

        public double Longitude => double.TryParse(Lon,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
    }
}
