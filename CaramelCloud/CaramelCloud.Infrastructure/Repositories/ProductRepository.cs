using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(AppDbContext db) : base(db) { }

        public void Update(Product entity)
        {
            var obj = _db.Products.FirstOrDefault(x => x.Id == entity.Id);
            if (obj is not null)
            {
                obj.Name = entity.Name;
                obj.Description = entity.Description;
                obj.Price = entity.Price;
                obj.CategoryId = entity.CategoryId;
                obj.RecipeId = entity.RecipeId;
                obj.Weight = entity.Weight;
                obj.ServingSize = entity.ServingSize;
                obj.IsAvailable = entity.IsAvailable;
                if (entity.ImageUrl != null)
                    obj.ImageUrl = entity.ImageUrl;
            }
        }
    }
}
