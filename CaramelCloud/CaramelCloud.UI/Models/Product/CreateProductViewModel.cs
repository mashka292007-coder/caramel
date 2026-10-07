using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.Product
{
    public class CreateProductViewModel
    {
        [DisplayName("Назва продукту")]
        [Required(ErrorMessage = "Поле не може бути пустим")]
        [MinLength(2, ErrorMessage = "Назва повинна бути більше 2 символів")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("Опис")]
        public string? Description { get; set; }

        [DisplayName("Ціна (грн)")]
        [Required(ErrorMessage = "Поле не може бути пустим")]
        [Range(0.01, 100000, ErrorMessage = "Ціна повинна бути більше 0")]
        public decimal Price { get; set; }

        [DisplayName("Категорія")]
        [Required(ErrorMessage = "Оберіть категорію")]
        public int CategoryId { get; set; }

        [DisplayName("Рецепт")]
        public int? RecipeId { get; set; }

        [DisplayName("Вага (кг)")]
        [Range(0.001, 1000, ErrorMessage = "Вага повинна бути більше 0")]
        public decimal? Weight { get; set; }

        [DisplayName("Розмір порції")]
        public int? ServingSize { get; set; }

        [DisplayName("Доступний")]
        public bool IsAvailable { get; set; } = true;

        [DisplayName("Зображення")]
        public string? ImageUrl { get; set; }

        public IFormFile? ImageFile { get; set; }
        public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Recipes { get; set; } = new List<SelectListItem>();
    }
}
