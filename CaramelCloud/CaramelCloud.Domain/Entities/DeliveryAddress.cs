using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class DeliveryAddress : BaseEntity<int>
    {
        public int UserId { get; set; }
        public string AddressLine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public bool IsDefault { get; set; } = false;
        public User User { get; set; } = null!;
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
