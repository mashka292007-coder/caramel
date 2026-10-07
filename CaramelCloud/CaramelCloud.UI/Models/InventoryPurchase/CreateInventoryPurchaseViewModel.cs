using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.InventoryPurchase
{
    public class CreateInventoryPurchaseViewModel
    {
        [DisplayName("Інгредієнт")]
        [Required(ErrorMessage = "Оберіть інгредієнт")]
        public int InventoryItemId { get; set; }

        [DisplayName("Кількість")]
        [Required(ErrorMessage = "Введіть кількість")]
        [Range(0.001, 999999, ErrorMessage = "Кількість повинна бути більше 0")]
        public decimal Quantity { get; set; }

        [DisplayName("Ціна за одиницю (грн)")]
        [Required(ErrorMessage = "Введіть ціну")]
        [Range(0.01, 999999, ErrorMessage = "Ціна повинна бути більше 0")]
        public decimal PricePerUnit { get; set; }

        [DisplayName("Постачальник")]
        [MaxLength(200)]
        public string? Supplier { get; set; }

        public IEnumerable<SelectListItem> InventoryItems { get; set; } = [];
    }
}
