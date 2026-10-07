using CaramelCloud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Interfaces
{
    public interface ICustomerProfileRepository : IRepository<CustomerProfile>
    {
        void Update(CustomerProfile entity);
    }
}
