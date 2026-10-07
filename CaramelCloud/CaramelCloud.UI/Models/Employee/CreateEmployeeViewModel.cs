using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.Employee
{
    public class CreateEmployeeViewModel
    {
        [DisplayName("Ім'я")]
        [Required(ErrorMessage = "Поле не може бути пустим")]
        [MinLength(2, ErrorMessage = "Ім'я повинно бути більше 2 символів")]
        public string FirstName { get; set; } = string.Empty;

        [DisplayName("Прізвище")]
        [Required(ErrorMessage = "Поле не може бути пустим")]
        [MinLength(2, ErrorMessage = "Прізвище повинно бути більше 2 символів")]
        public string LastName { get; set; } = string.Empty;

        [DisplayName("Телефон")]
        [Phone(ErrorMessage = "Невірний формат номера телефону")]
        public string? Phone { get; set; }

        [DisplayName("Посада")]
        [Required(ErrorMessage = "Оберіть посаду")]
        public int PositionId { get; set; }

        [DisplayName("Дата прийому")]
        public DateTime? HiredAt { get; set; }

        [DisplayName("Активний")]
        public bool IsActive { get; set; } = true;

        public string PositionName { get; set; } = "—";

        public IEnumerable<SelectListItem> Positions { get; set; } = new List<SelectListItem>();
    }
}
