using CaramelCloud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        void Update(Product entity);
    }
}
