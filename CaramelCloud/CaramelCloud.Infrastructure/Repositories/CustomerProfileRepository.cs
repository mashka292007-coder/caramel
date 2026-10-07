using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class CustomerProfileRepository : Repository<CustomerProfile>, ICustomerProfileRepository
    {
        public CustomerProfileRepository(AppDbContext db) : base(db) { }

        public void Update(CustomerProfile entity)
        {
            var obj = _db.CustomerProfiles.FirstOrDefault(x => x.Id == entity.Id);
            if (obj is not null)
            {
                obj.BirthDate = entity.BirthDate;
            }
        }
    }
}
