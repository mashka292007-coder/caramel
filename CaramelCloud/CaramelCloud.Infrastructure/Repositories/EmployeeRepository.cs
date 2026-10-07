using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(AppDbContext db) : base(db) { }

        public void Update(Employee entity)
        {
            var obj = _db.Employees.FirstOrDefault(x => x.Id == entity.Id);
            if (obj is not null)
            {
                obj.FirstName = entity.FirstName;
                obj.LastName = entity.LastName;
                obj.Phone = entity.Phone;
                obj.PositionId = entity.PositionId;
                obj.HiredAt = entity.HiredAt;
                obj.IsActive = entity.IsActive;
            }
        }
    }
}
