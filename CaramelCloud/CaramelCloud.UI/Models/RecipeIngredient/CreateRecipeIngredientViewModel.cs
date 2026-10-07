using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.RecipeIngredient
{
    public class CreateRecipeIngredientViewModel
    {
        [DisplayName("Рецепт")]
        [Required(ErrorMessage = "Оберіть рецепт")]
        public int RecipeId { get; set; }

        [DisplayName("Інгредієнт")]
        [Required(ErrorMessage = "Оберіть інгредієнт")]
        public int InventoryItemId { get; set; }

        [DisplayName("Кількість")]
        [Required(ErrorMessage = "Поле не може бути пустим")]
        [Range(0.001, double.MaxValue, ErrorMessage = "Кількість повинна бути більше 0")]
        public decimal Quantity { get; set; }
        public IEnumerable<SelectListItem> Recipes { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> InventoryItems { get; set; } = new List<SelectListItem>();
    }
}
