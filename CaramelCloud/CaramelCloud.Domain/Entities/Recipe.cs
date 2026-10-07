using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class Recipe : BaseEntity<int>
    {
        public string? Description { get; set; }
        public string? Notes { get; set; }
        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
        public Product? Product { get; set; }
        public CakeLayerType? CakeLayerType { get; set; }
        public CakeFillingType? CakeFillingType { get; set; }
    }
}
