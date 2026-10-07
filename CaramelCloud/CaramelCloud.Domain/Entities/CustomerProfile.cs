using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class CustomerProfile : BaseEntity<int>
    {
        public int UserId { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; } = null!;
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
