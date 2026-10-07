using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class AssignmentStatus : BaseEntity<int>
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<OrderAssignment> OrderAssignments { get; set; } = new List<OrderAssignment>();
    }
}
