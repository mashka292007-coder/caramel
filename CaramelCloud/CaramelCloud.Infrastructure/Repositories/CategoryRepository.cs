using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        public CategoryRepository(AppDbContext db) : base(db) { }

        public void Update(Category entity)
        {
            var obj = _db.Categories.FirstOrDefault(x => x.Id == entity.Id);
            if (obj is not null)
            {
                obj.Name = entity.Name;
                obj.Description = entity.Description;
                if (entity.ImageUrl != null)
                    obj.ImageUrl = entity.ImageUrl;
            }
        }
    }
}
