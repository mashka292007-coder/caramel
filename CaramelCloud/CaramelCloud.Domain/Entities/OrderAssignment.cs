using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class OrderAssignment : BaseEntity<int>
    {
        public int OrderId { get; set; }
        public int EmployeeId { get; set; }
        public int AssignmentTypeId { get; set; }
        public int AssignmentStatusId { get; set; }
        public int AssignedByUserId { get; set; }
        public string? Notes { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public Order Order { get; set; } = null!;
        public Employee Employee { get; set; } = null!;
        public AssignmentType AssignmentType { get; set; } = null!;
        public AssignmentStatus AssignmentStatus { get; set; } = null!;
        public User AssignedByUser { get; set; } = null!;
    }
}
