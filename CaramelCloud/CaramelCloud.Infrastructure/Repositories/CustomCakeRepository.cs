using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class CustomCakeRepository : Repository<CustomCake>, ICustomCakeRepository
    {
        public CustomCakeRepository(AppDbContext db) : base(db) { }

        public IEnumerable<CustomCake> GetByUser(int userId)
        {
            return _db.CustomCakes
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .Include(c => c.CustomCakeLayers)
                    .ThenInclude(l => l.LayerType)
                .Include(c => c.CustomCakeLayers)
                    .ThenInclude(l => l.FillingType)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();
        }

        public CustomCake? GetWithLayers(int cakeId)
        {
            return _db.CustomCakes
                .AsNoTracking()
                .Where(c => c.Id == cakeId)
                .Include(c => c.CustomCakeLayers.OrderBy(l => l.LayerOrder))
                    .ThenInclude(l => l.LayerType)
                .Include(c => c.CustomCakeLayers)
                    .ThenInclude(l => l.FillingType)
                .FirstOrDefault();
        }
    }
}
