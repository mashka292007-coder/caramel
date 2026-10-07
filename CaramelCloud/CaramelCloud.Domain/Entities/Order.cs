using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class Order : BaseEntity<int>
    {
        public int? CustomerProfileId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public int CreatedByUserId { get; set; }
        public int StatusId { get; set; }
        public int? PaymentMethodId { get; set; }
        public int? DeliveryMethodId { get; set; }
        public int? DeliveryAddressId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public CustomerProfile? CustomerProfile { get; set; }
        public User CreatedByUser { get; set; } = null!;
        public OrderStatus Status { get; set; } = null!;
        public PaymentMethod? PaymentMethod { get; set; }
        public DeliveryMethod? DeliveryMethod { get; set; }
        public DeliveryAddress? DeliveryAddress { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<OrderAssignment> OrderAssignments { get; set; } = new List<OrderAssignment>();
    }
}
