using CaramelCloud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.Interfaces
{
    public interface IInventoryPurchaseService
    {
        void AddPurchase(InventoryPurchase purchase);
        IEnumerable<string> GetLowStockWarnings();
        void RemovePurchase(InventoryPurchase purchase);
    }
}
