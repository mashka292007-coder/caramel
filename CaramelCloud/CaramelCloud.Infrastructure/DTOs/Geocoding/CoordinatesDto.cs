using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace CaramelCloud.Infrastructure.DTOs.Geocoding
{
    public class CoordinatesDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public bool IsFound => Latitude != 0 && Longitude != 0;

    }
}
