using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class InventoryItemRepository : Repository<InventoryItem>, IInventoryItemRepository
    {
        public InventoryItemRepository(AppDbContext db) : base(db) { }

        public void Update(InventoryItem entity)
        {
            var obj = _db.InventoryItems.FirstOrDefault(x => x.Id == entity.Id);
            if (obj is not null)
            {
                obj.ItemName = entity.ItemName;
                obj.Quantity = entity.Quantity;
                obj.Unit = entity.Unit;
                obj.MinQuantity = entity.MinQuantity;
                obj.LastUpdated = DateTime.UtcNow;
            }
        }
    }
}
