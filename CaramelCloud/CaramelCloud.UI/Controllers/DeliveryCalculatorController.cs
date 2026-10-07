using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using CaramelCloud.UI.Models.Delivery;
using Microsoft.AspNetCore.Mvc;

namespace CaramelCloud.UI.Controllers
{
    public class DeliveryCalculatorController : Controller
    {
        private readonly IGeocodingApiService _geocodingService;
        private readonly IDeliveryRouteApiService _routeService;
        private readonly ILogger<DeliveryCalculatorController> _logger;

        public DeliveryCalculatorController(
            IGeocodingApiService geocodingService,
            IDeliveryRouteApiService routeService,
            ILogger<DeliveryCalculatorController> logger)
        {
            _geocodingService = geocodingService;
            _routeService = routeService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index([FromQuery] string? address)
        {
            if (!string.IsNullOrWhiteSpace(address))
            {
                return View(new DeliveryCalculatorInputModel { Address = address });
            }
            return View(new DeliveryCalculatorInputModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calculate(DeliveryCalculatorInputModel model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            var result = await RunCalculation(model.Address);
            return View("Result", result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculateJson([FromForm] string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return BadRequest(new { error = "Адреса не вказана" });

            var result = await RunCalculation(address);

            return Ok(new
            {
                addressFound = result.AddressFound,
                foundDisplayName = result.FoundDisplayName,
                routeFound = result.RouteFound,
                distanceKm = result.DistanceKm,
                durationMinutes = result.DurationMinutes,
                estimatedDeliveryCost = result.EstimatedDeliveryCost,
                formattedDistance = result.FormattedDistance,
                formattedDuration = result.FormattedDuration,
                formattedCost = result.FormattedCost,
                fallbackMessage = result.FallbackMessage,
                clientLat = result.ClientLat,
                clientLon = result.ClientLon
            });
        }

        private async Task<DeliveryResultViewModel> RunCalculation(string address)
        {
            var result = new DeliveryResultViewModel { InputAddress = address };

            var coordinates = await _geocodingService.GetCoordinatesAsync(address);

            if (!coordinates.IsFound)
            {
                result.AddressFound = false;
                result.FallbackMessage =
                    "Адресу не знайдено. Спробуйте формат: \"місто, область\" або " +
                    "\"вулиця номер, місто\". Наприклад: \"Хмельницький, Хмельницька область\"";
                return result;
            }

            result.AddressFound = true;
            result.FoundDisplayName = coordinates.DisplayName;
            result.ClientLat = coordinates.Latitude;
            result.ClientLon = coordinates.Longitude;

            var route = await _routeService.CalculateRouteAsync(coordinates);

            if (route.IsFound)
            {
                result.RouteFound = true;
                result.DistanceKm = route.DistanceKm;
                result.DurationMinutes = route.DurationMinutes;
                result.EstimatedDeliveryCost = route.EstimatedDeliveryCost;
            }
            else
            {
                result.RouteFound = false;
                result.FallbackMessage =
                    "Точний маршрут тимчасово недоступний. " +
                    "Зателефонуйте нам для уточнення деталей доставки.";
            }

            return result;
        }
    }
}
