using CaramelCloud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.Interfaces
{
    public interface IOrderService
    {
        Task<int> CreateOrderAsync(Order order, List<(int productId, int quantity)> items);
        void UpdateOrder(Order order);
        Task UpdateStatusAsync(int orderId, int statusId, int assignedByUserId);
    }
}
