using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class Position : BaseEntity<int>
    {
        public string Name { get; set; } = string.Empty;
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
