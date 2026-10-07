using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.InventoryItem
{
    public class CreateInventoryItemViewModel
    {
        [DisplayName("Назва")]
        [Required(ErrorMessage = "Поле не може бути пустим")]
        [MinLength(2, ErrorMessage = "Назва повинна бути більше 2 символів")]
        public string ItemName { get; set; } = string.Empty;

        [DisplayName("Кількість")]
        [Required(ErrorMessage = "Поле не може бути пустим")]
        [Range(0, double.MaxValue, ErrorMessage = "Кількість не може бути від'ємною")]
        public decimal Quantity { get; set; }

        [DisplayName("Одиниця виміру")]
        [Required(ErrorMessage = "Поле не може бути пустим")]
        public string Unit { get; set; } = string.Empty;

        [DisplayName("Мінімальна кількість")]
        [Required(ErrorMessage = "Поле не може бути пустим")]
        [Range(0, double.MaxValue, ErrorMessage = "Мінімальна кількість не може бути від'ємною")]
        public decimal MinQuantity { get; set; }
    }
}
