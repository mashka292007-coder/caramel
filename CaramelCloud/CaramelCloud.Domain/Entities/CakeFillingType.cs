using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class CakeFillingType : BaseEntity<int>
    {
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal ExtraPrice { get; set; }
        public string? Description { get; set; }
        public int? RecipeId { get; set; }
        public bool IsAvailable { get; set; } = true;
        public Recipe? Recipe { get; set; }
        public ICollection<CustomCakeLayer> CustomCakeLayers { get; set; } = new List<CustomCakeLayer>();
    }
}
