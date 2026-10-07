using CaramelCloud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Interfaces
{
    public interface ICategoryRepository : IRepository<Category>
    {
        void Update(Category entity);
    }
}
