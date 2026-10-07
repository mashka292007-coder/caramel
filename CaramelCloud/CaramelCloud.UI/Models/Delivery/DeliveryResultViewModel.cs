namespace CaramelCloud.UI.Models.Delivery
{
    public class DeliveryResultViewModel
    {
        public string InputAddress { get; set; } = string.Empty;

        public bool AddressFound { get; set; }
        public string? FoundDisplayName { get; set; }
        public double? ClientLat { get; set; }
        public double? ClientLon { get; set; }

        public bool RouteFound { get; set; }
        public double? DistanceKm { get; set; }
        public int? DurationMinutes { get; set; }
        public decimal? EstimatedDeliveryCost { get; set; }

        public string ShopAddress { get; } = "м. Острог, просп. Незалежності 60";
        public double ShopLat { get; } = 50.3280;
        public double ShopLon { get; } = 26.5148;

        public string FormattedDistance => DistanceKm.HasValue ? $"{DistanceKm:F1} км" : "—";
        public string FormattedDuration => DurationMinutes.HasValue
            ? (DurationMinutes >= 60
                ? $"{DurationMinutes / 60} год {DurationMinutes % 60} хв"
                : $"{DurationMinutes} хв")
            : "—";
        public string FormattedCost => EstimatedDeliveryCost.HasValue
            ? $"{EstimatedDeliveryCost:N0} ₴"
            : "—";

        public bool HasResult => AddressFound;
        public string? FallbackMessage { get; set; }
    }
}
