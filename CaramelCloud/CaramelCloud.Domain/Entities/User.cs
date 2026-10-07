using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace CaramelCloud.Domain.Entities
{
    public class User : IdentityUser<int>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? RoleId { get; set; }         
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public Role? Role { get; set; }
        public CustomerProfile? CustomerProfile { get; set; }
        public ICollection<DeliveryAddress> DeliveryAddresses { get; set; } = new List<DeliveryAddress>();
        public ICollection<Order> CreatedOrders { get; set; } = new List<Order>();
        public ICollection<OrderAssignment> AssignedByAssignments { get; set; } = new List<OrderAssignment>();
        public ICollection<CustomCake> CustomCakes { get; set; } = new List<CustomCake>();
    }
}
