using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class CustomCake : BaseEntity<int>
    {
        public int UserId { get; set; }
        public string? Name { get; set; }
        public int LayerCount { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; } = null!;
        public ICollection<CustomCakeLayer> CustomCakeLayers { get; set; } = new List<CustomCakeLayer>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
