using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class InventoryPurchase : BaseEntity<int>
    {
        public int InventoryItemId { get; set; }
        public decimal Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public string? Supplier { get; set; }
        public InventoryItem InventoryItem { get; set; } = null!;
    }
}
