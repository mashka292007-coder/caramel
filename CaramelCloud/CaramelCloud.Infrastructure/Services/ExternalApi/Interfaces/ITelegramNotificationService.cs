using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces
{
    public interface ITelegramNotificationService
    {
        Task SendNewOrderNotificationAsync(
            int orderId,
            string customerName,
            string customerPhone,
            decimal totalAmount,
            string? comment,
            IEnumerable<string> itemNames,
            string? deliveryMethod = null,
            string? deliveryAddress = null,
            DateTime? deliveryDate = null,
            bool isDelivery = false,
            string? paymentMethod = null);

    }
}
