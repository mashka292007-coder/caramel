using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class RecipeIngredient : BaseEntity<int>
    {
        public int RecipeId { get; set; }
        public int InventoryItemId { get; set; }
        public decimal Quantity { get; set; }
        public Recipe Recipe { get; set; } = null!;
        public InventoryItem InventoryItem { get; set; } = null!;
    }
}
