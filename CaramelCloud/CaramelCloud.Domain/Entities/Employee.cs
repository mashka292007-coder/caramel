using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class Employee : BaseEntity<int>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public int PositionId { get; set; }
        public DateTime? HiredAt { get; set; }
        public bool IsActive { get; set; } = true;
        public Position Position { get; set; } = null!;
        public ICollection<OrderAssignment> OrderAssignments { get; set; } = new List<OrderAssignment>();
    }
}
