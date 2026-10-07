using CaramelCloud.Infrastructure.DTOs.Geocoding;
using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Impl
{
    public class GeocodingApiService : IGeocodingApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<GeocodingApiService> _logger;

        public GeocodingApiService(
            HttpClient httpClient,
            IMemoryCache cache,
            ILogger<GeocodingApiService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<CoordinatesDto> GetCoordinatesAsync(string address)
        {
            var cacheKey = $"nominatim:{address.ToLower().Trim()}";

            if (_cache.TryGetValue(cacheKey, out CoordinatesDto? cached))
                return cached!;

            try
            {
                var query = Uri.EscapeDataString(address);
                var response = await _httpClient.GetAsync(
                    $"search?q={query}&format=json&limit=1&countrycodes=ua&addressdetails=1");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<NominatimResultDto>>(json);

                if (results is null || results.Count == 0)
                {
                    _logger.LogWarning("Nominatim не знайшов адресу: {Address}", address);
                    return new CoordinatesDto();
                }

                var first = results[0];
                var coordinates = new CoordinatesDto
                {
                    Latitude = first.Latitude,
                    Longitude = first.Longitude,
                    DisplayName = first.DisplayName
                };

                _cache.Set(cacheKey, coordinates, TimeSpan.FromHours(24));
                _logger.LogInformation("Nominatim знайшов координати для: {Address}", address);
                return coordinates;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Nominatim недоступний для: {Address}", address);
                return new CoordinatesDto();
            }
        }

    }
}
