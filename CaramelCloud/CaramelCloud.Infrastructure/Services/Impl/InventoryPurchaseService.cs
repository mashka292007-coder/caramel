using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.Impl
{
    public class InventoryPurchaseService : IInventoryPurchaseService
    {
        private readonly IUnitOfWork _uow;

        public InventoryPurchaseService(IUnitOfWork uow) => _uow = uow;

        public void AddPurchase(InventoryPurchase purchase)
        {
            _uow.InventoryPurchase.AddPurchaseAndUpdateStock(purchase);
            _uow.Save();
        }

        public void RemovePurchase(InventoryPurchase purchase)
        {
            var invItem = purchase.InventoryItem;

            if (invItem is not null)
            {
                invItem.Quantity = Math.Max(0, invItem.Quantity - purchase.Quantity);
                invItem.LastUpdated = DateTime.UtcNow;
            }

            _uow.InventoryPurchase.Remove(purchase);
            _uow.Save();
        }
        public IEnumerable<string> GetLowStockWarnings() =>
            _uow.InventoryItem
                .GetAll(i => i.Quantity < i.MinQuantity)
                .Select(i => $"{i.ItemName}: залишок {i.Quantity} {i.Unit} (мін. {i.MinQuantity} {i.Unit})")
                .ToList();
    }
}
