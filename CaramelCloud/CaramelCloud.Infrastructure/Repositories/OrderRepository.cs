using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(AppDbContext db) : base(db) { }

        public void Update(Order entity)
        {
            var obj = _db.Orders.FirstOrDefault(x => x.Id == entity.Id);
            if (obj is not null)
            {
                obj.CustomerName = entity.CustomerName;
                obj.CustomerPhone = entity.CustomerPhone;
                obj.StatusId = entity.StatusId;
                obj.PaymentMethodId = entity.PaymentMethodId;
                obj.DeliveryMethodId = entity.DeliveryMethodId;
                obj.DeliveryAddressId = entity.DeliveryAddressId;
                obj.DeliveryDate = entity.DeliveryDate;
                obj.Comment = entity.Comment;
                obj.TotalAmount = entity.TotalAmount;
                obj.UpdatedAt = DateTime.UtcNow;
            }
        }

        public void UpdateStatus(int orderId, int statusId)
        {
            var obj = _db.Orders.FirstOrDefault(x => x.Id == orderId);
            if (obj is not null)
            {
                obj.StatusId = statusId;
                obj.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
