using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class InventoryPurchaseRepository : Repository<InventoryPurchase>, IInventoryPurchaseRepository
    {
        public InventoryPurchaseRepository(AppDbContext db) : base(db) { }
        public void AddPurchaseAndUpdateStock(InventoryPurchase purchase)
        {
            purchase.TotalCost = purchase.Quantity * purchase.PricePerUnit;
            purchase.PurchaseDate = DateTime.UtcNow;
            _db.InventoryPurchases.Add(purchase);

            var item = _db.InventoryItems.FirstOrDefault(i => i.Id == purchase.InventoryItemId);
            if (item is not null)
            {
                item.Quantity += purchase.Quantity;
                item.LastUpdated = DateTime.UtcNow;
            }
        }
    }
}
