using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class RecipeIngredientRepository : Repository<RecipeIngredient>, IRecipeIngredientRepository
    {
        public RecipeIngredientRepository(AppDbContext db) : base(db) { }

        public void Update(RecipeIngredient entity)
        {
            var obj = _db.RecipeIngredients.FirstOrDefault(x => x.Id == entity.Id);
            if (obj is not null)
            {
                obj.RecipeId = entity.RecipeId;
                obj.InventoryItemId = entity.InventoryItemId;
                obj.Quantity = entity.Quantity;
            }
        }
    }
}
