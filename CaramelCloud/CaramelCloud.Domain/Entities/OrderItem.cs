using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class OrderItem : BaseEntity<int>
    {
        public int OrderId { get; set; }
        public int? ProductId { get; set; }
        public int? CustomCakeId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public Order Order { get; set; } = null!;
        public Product? Product { get; set; }
        public CustomCake? CustomCake { get; set; }
    }
}
