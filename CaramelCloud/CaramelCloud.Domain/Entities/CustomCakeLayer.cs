using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Entities
{
    public class CustomCakeLayer : BaseEntity<int>
    {
        public int CustomCakeId { get; set; }
        public int LayerTypeId { get; set; }
        public int FillingTypeId { get; set; }
        public int LayerOrder { get; set; }
        public CustomCake CustomCake { get; set; } = null!;
        public CakeLayerType LayerType { get; set; } = null!;
        public CakeFillingType FillingType { get; set; } = null!;
    }
}
