using CaramelCloud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Interfaces
{
    public interface IRoleRepository : IRepository<Role>
    {
        Role? GetByName(string name);
    }
}
