using CaramelCloud.Infrastructure.DTOs.Geocoding;
using CaramelCloud.Infrastructure.DTOs.Route;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces
{
    public interface IDeliveryRouteApiService
    {
        Task<RouteInfoDto> CalculateRouteAsync(CoordinatesDto destination);
    }
}
