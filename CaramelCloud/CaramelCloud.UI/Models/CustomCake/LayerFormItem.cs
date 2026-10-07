using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.CustomCake
{
    public class LayerFormItem
    {
        [Required]
        public int LayerOrder { get; set; }

        [Required]
        public int LayerTypeId { get; set; }

        [Required]
        public int FillingTypeId { get; set; }
    }
}
