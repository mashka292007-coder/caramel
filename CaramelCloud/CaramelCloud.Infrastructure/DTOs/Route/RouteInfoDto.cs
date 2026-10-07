using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.DTOs.Route
{
    public class RouteInfoDto
    {
        public double DistanceKm { get; set; }
        public int DurationMinutes { get; set; }
        public decimal EstimatedDeliveryCost { get; set; }
        public bool IsFound => DistanceKm > 0;

        public string FormattedDistance => $"{DistanceKm:F1} км";
        public string FormattedDuration => DurationMinutes >= 60
            ? $"{DurationMinutes / 60} год {DurationMinutes % 60} хв"
            : $"{DurationMinutes} хв";

    }
}
