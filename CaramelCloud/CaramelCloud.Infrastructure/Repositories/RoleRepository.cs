using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class RoleRepository : Repository<Role>, IRoleRepository
    {
        public RoleRepository(AppDbContext db) : base(db) { }

        public Role? GetByName(string name) =>
            _db.Roles.FirstOrDefault(r => r.Name == name);
    }
}
