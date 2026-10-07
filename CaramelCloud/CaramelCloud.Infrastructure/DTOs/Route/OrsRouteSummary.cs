using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace CaramelCloud.Infrastructure.DTOs.Route
{
    public class OrsRouteSummary
    {
        [JsonPropertyName("distance")]
        public double Distance { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }

    }
}
