using CaramelCloud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        void Update(Order entity);
        void UpdateStatus(int orderId, int statusId);
    }
}
