using CaramelCloud.Infrastructure.DTOs.Geocoding;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces
{
    public interface IGeocodingApiService
    {
        Task<CoordinatesDto> GetCoordinatesAsync(string address);
    }
}
