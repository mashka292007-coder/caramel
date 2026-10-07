using CaramelCloud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Interfaces
{
    public interface IRecipeRepository : IRepository<Recipe>
    {
        void Update(Recipe entity);
    }
}
