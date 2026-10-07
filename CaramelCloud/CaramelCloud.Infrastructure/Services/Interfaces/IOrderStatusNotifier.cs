using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.Interfaces
{
    public interface IOrderStatusNotifier
    {
        Task NotifyOrderStatusChangedAsync(int userId, int orderId, string newStatus);
    }
}
