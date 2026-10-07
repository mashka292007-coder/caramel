using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.CustomCake
{
    public class BuildCakeFormModel
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [Required]
        public decimal BasePrice { get; set; }  

        [Required]
        [MinLength(1, ErrorMessage = "Додайте хоча б один шар")]
        public List<LayerFormItem> Layers { get; set; } = new();
    }
}
