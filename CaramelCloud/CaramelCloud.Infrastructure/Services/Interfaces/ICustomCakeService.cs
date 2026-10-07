using CaramelCloud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.Interfaces
{
    public interface ICustomCakeService
    {
        Task<int> BuildCakeAsync(CustomCake cake, List<CakeLayerInput> layers);
        Task AddCakeToOrderAsync(int orderId, int customCakeId, int quantity = 1);

        Task<int> PlaceOrderWithCakeAsync(
        int userId,
        int customCakeId,
        int? deliveryAddressId,
        int? deliveryMethodId,
        int? paymentMethodId,
        DateTime? deliveryDate,
        string? comment,
        decimal deliveryCost = 0);
        void DeleteCake(int cakeId);

        decimal CalculatePrice(decimal basePrice, List<CakeLayerInput> layers);
    }
    public record CakeLayerInput(
        int LayerOrder,
        int LayerTypeId,
        int FillingTypeId);
}
