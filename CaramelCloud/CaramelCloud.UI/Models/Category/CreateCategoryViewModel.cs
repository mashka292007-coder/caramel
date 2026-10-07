using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.Category
{
    public class CreateCategoryViewModel
    {
        [DisplayName("Назва категорії")]
        [Required(ErrorMessage = "Поле не може бути пустим")]
        [MinLength(3, ErrorMessage = "Назва повинна бути більше 3 символів")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("Опис")]
        public string? Description { get; set; }

        [DisplayName("Зображення")]
        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}
