using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class RecipeRepository : Repository<Recipe>, IRecipeRepository
    {
        public RecipeRepository(AppDbContext db) : base(db) { }

        public void Update(Recipe entity)
        {
            var obj = _db.Recipes.FirstOrDefault(x => x.Id == entity.Id);
            if (obj is not null)
            {
                obj.Description = entity.Description;
                obj.Notes = entity.Notes;
            }
        }
    }
}
