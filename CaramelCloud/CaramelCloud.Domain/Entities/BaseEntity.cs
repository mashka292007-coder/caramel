using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public abstract class BaseEntity<TKey>
    {
        public TKey Id { get; set; } = default!;
    }
}
