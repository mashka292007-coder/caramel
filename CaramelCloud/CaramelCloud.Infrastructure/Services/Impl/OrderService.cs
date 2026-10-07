using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;
using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using CaramelCloud.Infrastructure.Settings;

namespace CaramelCloud.Infrastructure.Services.Impl
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITelegramNotificationService _telegram;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IUnitOfWork uow,
            ITelegramNotificationService telegram,
            ILogger<OrderService> logger)
        {
            _uow = uow;
            _telegram = telegram;
            _logger = logger;
        }

        public async Task<int> CreateOrderAsync(Order order, List<(int productId, int quantity)> items)
        {
            _uow.Order.Add(order);
            await _uow.SaveAsync();

            var itemNames = new List<string>();

            foreach (var (productId, quantity) in items)
            {
                var product = _uow.Product.Get(p => p.Id == productId);
                if (product is null) continue;

                _uow.OrderItem.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                });

                itemNames.Add($"{product.Name} x {quantity} = {product.Price * quantity:N0} {TelegramEmoji.Hryvnia}");
            }

            await _uow.SaveAsync();

            var saved = _uow.Order.Get(o => o.Id == order.Id, tracked: true);
            if (saved is not null)
            {
                saved.TotalAmount = _uow.OrderItem
                    .GetAll(oi => oi.OrderId == order.Id)
                    .Sum(oi => oi.UnitPrice * oi.Quantity) + order.TotalAmount;
                saved.UpdatedAt = DateTime.UtcNow;
            }
            await _uow.SaveAsync();

            var orderFull = _uow.Order.Get(
                o => o.Id == order.Id,
                includeProperties: "CustomerProfile.User,DeliveryMethod,DeliveryAddress,PaymentMethod");

            var customerName = ResolveCustomerName(orderFull);
            var customerPhone = ResolvePhone(orderFull);
            var deliveryMethod = orderFull?.DeliveryMethod?.Name;
            var deliveryAddress = orderFull?.DeliveryAddress is not null
                ? $"{orderFull.DeliveryAddress.City}, {orderFull.DeliveryAddress.AddressLine}"
                : null;
            bool isDelivery = order.DeliveryMethodId.HasValue && order.DeliveryMethodId.Value != 1;

            try
            {
                await _telegram.SendNewOrderNotificationAsync(
                    orderId: order.Id,
                    customerName: customerName,
                    customerPhone: customerPhone,
                    totalAmount: saved?.TotalAmount ?? order.TotalAmount,
                    comment: order.Comment,
                    itemNames: itemNames,
                    deliveryMethod: deliveryMethod,
                    deliveryAddress: deliveryAddress,
                    deliveryDate: order.DeliveryDate,
                    isDelivery: isDelivery,
                    paymentMethod: orderFull?.PaymentMethod?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Telegram сповіщення не надіслано для замовлення #{OrderId}", order.Id);
            }

            return order.Id;
        }

        public void UpdateOrder(Order order)
        {
            _uow.Order.Update(order);
            _uow.Save();
        }

        public async Task UpdateStatusAsync(int orderId, int statusId, int assignedByUserId)
        {
            var currentOrder = _uow.Order.Get(o => o.Id == orderId, tracked: true);
            if (currentOrder is null) return;

            int previousStatusId = currentOrder.StatusId;
            if (previousStatusId == statusId) return;

            _uow.Order.UpdateStatus(orderId, statusId);

            if (statusId == 2 && previousStatusId != 2)
            {
                var hasExistingAssignment = _uow.OrderAssignment
                    .GetAll(a => a.OrderId == orderId && a.AssignmentTypeId == 1 && a.CompletedAt == null)
                    .Any();

                if (!hasExistingAssignment)
                {
                    const int confectionerPositionId = 1;
                    var employee = _uow.OrderAssignment.GetLeastBusyEmployee(confectionerPositionId);

                    if (employee is not null)
                    {
                        _uow.OrderAssignment.Add(new OrderAssignment
                        {
                            OrderId = orderId,
                            EmployeeId = employee.Id,
                            AssignmentTypeId = 1,
                            AssignmentStatusId = 1,
                            AssignedByUserId = assignedByUserId,
                            AssignedAt = DateTime.UtcNow,
                        });
                    }
                }

                AdjustInventoryForOrder(orderId, deduct: true);
            }

            if (statusId == 5 && previousStatusId == 2)
            {
                AdjustInventoryForOrder(orderId, deduct: false);
            }

            await _uow.SaveAsync();
        }

      
        private void AdjustInventoryForOrder(int orderId, bool deduct)
        {
            var items = _uow.OrderItem.GetAll(
                oi => oi.OrderId == orderId,
                includeProperties: "Product,CustomCake,CustomCake.CustomCakeLayers");

            foreach (var item in items)
            {
                if (item.ProductId is not null)
                {
                    var product = _uow.Product.Get(
                        p => p.Id == item.ProductId.Value,
                        includeProperties: "Recipe");

                    if (product?.RecipeId is not null)
                        AdjustByRecipe(product.RecipeId.Value, item.Quantity, deduct);
                }

                if (item.CustomCakeId is not null)
                {
                    var layers = _uow.CustomCakeLayer.GetAll(
                        l => l.CustomCakeId == item.CustomCakeId.Value,
                        includeProperties: "LayerType,FillingType");

                    foreach (var layer in layers)
                    {
                        if (layer.LayerType?.RecipeId is not null)
                            AdjustByRecipe(layer.LayerType.RecipeId.Value, item.Quantity, deduct);

                        if (layer.FillingType?.RecipeId is not null)
                            AdjustByRecipe(layer.FillingType.RecipeId.Value, item.Quantity, deduct);
                    }
                }
            }
        }

        private void AdjustByRecipe(int recipeId, int quantity, bool deduct)
        {
            var ingredients = _uow.RecipeIngredient
                .GetAll(ri => ri.RecipeId == recipeId);

            foreach (var ingredient in ingredients)
            {
                var invItem = _uow.InventoryItem.Get(
                    i => i.Id == ingredient.InventoryItemId,
                    tracked: true);

                if (invItem is null) continue;

                var delta = ingredient.Quantity * quantity;
                invItem.Quantity = deduct
                    ? Math.Max(0, invItem.Quantity - delta)
                    : invItem.Quantity + delta;
                invItem.LastUpdated = DateTime.UtcNow;
            }
        }

        private static string ResolveCustomerName(Order? o)
        {
            if (o is null) return "Клієнт";
            if (!string.IsNullOrEmpty(o.CustomerName)) return o.CustomerName;
            if (o.CustomerProfile?.User is { } u)
                return $"{u.FirstName} {u.LastName}".Trim();
            return "Клієнт";
        }

        private static string ResolvePhone(Order? o)
        {
            if (o is null) return "—";
            if (!string.IsNullOrWhiteSpace(o.CustomerPhone)) return o.CustomerPhone;
            return o.CustomerProfile?.User?.PhoneNumber ?? "—";
        }
    }
}