using CaramelCloud.Infrastructure.DTOs.Geocoding;
using CaramelCloud.Infrastructure.DTOs.Route;
using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Impl
{
    public class DeliveryRouteApiService : IDeliveryRouteApiService
    {

        private readonly HttpClient _httpClient;
        private readonly ILogger<DeliveryRouteApiService> _logger;

        private const double ShopLat = 50.3280;
        private const double ShopLon = 26.5148;

        private const decimal BasePrice = 50m;
        private const decimal PricePerKm = 15m;
        private const double FreeDistanceKm = 3.0;

        public DeliveryRouteApiService(
            HttpClient httpClient,
            ILogger<DeliveryRouteApiService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;

            var apiKey = configuration["ApiKeys:OpenRouteService"]
                ?? throw new InvalidOperationException("ApiKeys:OpenRouteService не налаштовано");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public async Task<RouteInfoDto> CalculateRouteAsync(CoordinatesDto destination)
        {
            if (!destination.IsFound)
                return new RouteInfoDto();

            try
            {
                var body = new
                {
                    coordinates = new[]
                    {
                    new[] { ShopLon, ShopLat },
                    new[] { destination.Longitude, destination.Latitude }
                }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("v2/directions/driving-car", content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var orsResult = JsonSerializer.Deserialize<OrsDirectionsDto>(json);

                if (orsResult?.Routes is null || orsResult.Routes.Count == 0)
                    return new RouteInfoDto();

                var summary = orsResult.Routes[0].Summary;
                double distanceKm = summary.Distance / 1000.0;
                int durationMin = (int)Math.Ceiling(summary.Duration / 60.0);

                decimal cost = BasePrice;
                if (distanceKm > FreeDistanceKm)
                    cost += (decimal)(distanceKm - FreeDistanceKm) * PricePerKm;

                return new RouteInfoDto
                {
                    DistanceKm = Math.Round(distanceKm, 1),
                    DurationMinutes = durationMin,
                    EstimatedDeliveryCost = Math.Round(cost, 0)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenRouteService недоступний — fallback без маршруту");
                return new RouteInfoDto();
            }

        }
    }
}
