using System.ComponentModel;

namespace CaramelCloud.UI.Models.Recipe
{
    public class CreateRecipeViewModel
    {
        [DisplayName("Опис")]
        public string? Description { get; set; }

        [DisplayName("Нотатки")]
        public string? Notes { get; set; }
    }
}
