using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class InventoryItem : BaseEntity<int>
    {
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal MinQuantity { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
        public ICollection<InventoryPurchase> Purchases { get; set; } = new List<InventoryPurchase>();
    }
}
