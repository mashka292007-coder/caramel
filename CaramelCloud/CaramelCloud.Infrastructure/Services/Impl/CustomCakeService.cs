using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using CaramelCloud.Infrastructure.Services.Interfaces;
using CaramelCloud.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.Impl
{
    public class CustomCakeService : ICustomCakeService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITelegramNotificationService _telegram; 
        private readonly ILogger<CustomCakeService> _logger;
        public CustomCakeService(
        IUnitOfWork uow,
        ITelegramNotificationService telegram,               
        ILogger<CustomCakeService> logger)                  
        {
            _uow = uow;
            _telegram = telegram;
            _logger = logger;
        }

        public async Task<int> BuildCakeAsync(CustomCake cake, List<CakeLayerInput> layers)
        {
            if (layers is null || layers.Count == 0)
                throw new ArgumentException("Торт повинен мати хоча б один шар.");

            cake.LayerCount = layers.Count;
            cake.TotalPrice = CalculatePrice(cake.TotalPrice, layers);
            cake.CreatedAt = DateTime.UtcNow;

            _uow.CustomCake.Add(cake);
            await _uow.SaveAsync();

            foreach (var input in layers)
            {
                _uow.CustomCakeLayer.Add(new CustomCakeLayer
                {
                    CustomCakeId = cake.Id,
                    LayerOrder = input.LayerOrder,
                    LayerTypeId = input.LayerTypeId,
                    FillingTypeId = input.FillingTypeId,
                });
            }

            await _uow.SaveAsync();
            return cake.Id;
        }

        public async Task AddCakeToOrderAsync(int orderId, int customCakeId, int quantity = 1)
        {
            var cake = _uow.CustomCake.Get(c => c.Id == customCakeId);
            if (cake is null)
                throw new InvalidOperationException($"Торт {customCakeId} не знайдено.");

            var order = _uow.Order.Get(o => o.Id == orderId, tracked: true);
            if (order is null)
                throw new InvalidOperationException($"Замовлення {orderId} не знайдено.");

            _uow.OrderItem.Add(new OrderItem
            {
                OrderId = orderId,
                CustomCakeId = customCakeId,
                ProductId = null,
                Quantity = quantity,
                UnitPrice = cake.TotalPrice,
            });

            order.TotalAmount += cake.TotalPrice * quantity;
            order.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveAsync();
        }

        public async Task<int> PlaceOrderWithCakeAsync(
            int userId,
            int customCakeId,
            int? deliveryAddressId,
            int? deliveryMethodId,
            int? paymentMethodId,
            DateTime? deliveryDate,
            string? comment,
            decimal deliveryCost = 0)
        {
            var cake = _uow.CustomCake.Get(c => c.Id == customCakeId);
            if (cake is null)
                throw new InvalidOperationException($"Торт {customCakeId} не знайдено.");

            var customerProfile = _uow.CustomerProfile.Get(cp => cp.UserId == userId);

            var order = new Order
            {
                CreatedByUserId = userId,
                CustomerProfileId = customerProfile?.Id,
                StatusId = 1,
                PaymentMethodId = paymentMethodId,
                DeliveryMethodId = deliveryMethodId,
                DeliveryAddressId = deliveryAddressId,
                DeliveryDate = deliveryDate,
                Comment = comment?.Trim(),
                TotalAmount = cake.TotalPrice + deliveryCost,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _uow.Order.Add(order);
            await _uow.SaveAsync();

            _uow.OrderItem.Add(new OrderItem
            {
                OrderId = order.Id,
                CustomCakeId = customCakeId,
                ProductId = null,
                Quantity = 1,
                UnitPrice = cake.TotalPrice,
            });

            await _uow.SaveAsync();

            try
            {
                var profileWithUser = _uow.CustomerProfile.Get(
                    cp => cp.UserId == userId,
                    includeProperties: "User");

                var customerName = profileWithUser?.User is { } u
                    ? $"{u.FirstName} {u.LastName}".Trim()
                    : "Клієнт";

                var customerPhone = profileWithUser?.User?.PhoneNumber ?? "—";

                var deliveryAddress = deliveryAddressId.HasValue
                    ? _uow.DeliveryAddress.Get(a => a.Id == deliveryAddressId.Value) is { } addr
                        ? $"{addr.City}, {addr.AddressLine}"
                        : null
                    : null;

                bool isDelivery = deliveryMethodId.HasValue && deliveryMethodId.Value != 1;
                var paymentMethodName = order.PaymentMethodId.HasValue
                    ? _uow.Order.Get(
                        o => o.Id == order.Id,
                        includeProperties: "PaymentMethod")?.PaymentMethod?.Name
                    : null;

                await _telegram.SendNewOrderNotificationAsync(
                    orderId: order.Id,
                    customerName: customerName,
                    customerPhone: customerPhone,
                    totalAmount: cake.TotalPrice,
                    comment: comment,
                    itemNames: new[] { $"{TelegramEmoji.Cake} {cake.Name} (конструктор) x 1 = {cake.TotalPrice:N0} {TelegramEmoji.Hryvnia}" },
                    deliveryMethod: isDelivery ? "Кур'єр" : "Самовивіз",
                    deliveryAddress: deliveryAddress,
                    deliveryDate: deliveryDate,
                    isDelivery: isDelivery,
                    paymentMethod: paymentMethodName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram сповіщення не надіслано для замовлення #{OrderId}", order.Id);
            }

            return order.Id;
        }

        public void DeleteCake(int cakeId)
        {
            var isInOrder = _uow.OrderItem
                .GetAll(oi => oi.CustomCakeId == cakeId)
                .Any();

            if (isInOrder)
                throw new InvalidOperationException(
                    "Неможливо видалити торт — він вже доданий до замовлення.");

            var layers = _uow.CustomCakeLayer
                .GetAll(l => l.CustomCakeId == cakeId)
                .ToList();

            _uow.CustomCakeLayer.RemoveRange(layers);

            var cake = _uow.CustomCake.Get(c => c.Id == cakeId, tracked: true);
            if (cake is not null)
                _uow.CustomCake.Remove(cake);

            _uow.Save();
        }

        public decimal CalculatePrice(decimal basePrice, List<CakeLayerInput> layers)
        {
            var layerTypeIds = layers.Select(l => l.LayerTypeId).Distinct().ToList();
            var fillingTypeIds = layers.Select(l => l.FillingTypeId).Distinct().ToList();

            var layerPrices = _uow.CakeLayerType
                .GetAll(lt => layerTypeIds.Contains(lt.Id))
                .ToDictionary(lt => lt.Id, lt => lt.ExtraPrice);

            var fillingPrices = _uow.CakeFillingType
                .GetAll(ft => fillingTypeIds.Contains(ft.Id))
                .ToDictionary(ft => ft.Id, ft => ft.ExtraPrice);

            var extra = layers.Sum(l =>
                layerPrices.GetValueOrDefault(l.LayerTypeId)
              + fillingPrices.GetValueOrDefault(l.FillingTypeId));

            return basePrice + extra;
        }
    }
}
