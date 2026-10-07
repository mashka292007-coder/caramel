using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class DeliveryMethod : BaseEntity<int>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
