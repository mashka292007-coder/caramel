using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace CaramelCloud.Infrastructure.DTOs.Route
{
    public class OrsRoute
    {
        [JsonPropertyName("summary")]
        public OrsRouteSummary Summary { get; set; } = new();

    }
}
