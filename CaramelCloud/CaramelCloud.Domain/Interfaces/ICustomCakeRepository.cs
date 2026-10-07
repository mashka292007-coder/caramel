using CaramelCloud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Interfaces
{
    public interface ICustomCakeRepository : IRepository<CustomCake>
    {
        IEnumerable<CustomCake> GetByUser(int userId);

        CustomCake? GetWithLayers(int cakeId);
    }
}
