using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class PositionRepository : Repository<Position>, IPositionRepository
    {
        public PositionRepository(AppDbContext db) : base(db) { }
    }
}
