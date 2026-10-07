using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace CaramelCloud.Infrastructure.DTOs.Route
{
    public class OrsDirectionsDto
    {
        [JsonPropertyName("routes")]
        public List<OrsRoute> Routes { get; set; } = new();

    }
}
