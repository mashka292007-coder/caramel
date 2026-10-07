using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.InstagramThreads
{
    public class InstagramThreadsPostViewModel
    {
        [Required(ErrorMessage = "Оберіть зображення")]
        public string ImageUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть підпис до посту")]
        [MaxLength(2200, ErrorMessage = "Максимум 2200 символів")]
        public string Caption { get; set; } = string.Empty;

        public List<SelectListItem> Products { get; set; } = new();
    }
}
