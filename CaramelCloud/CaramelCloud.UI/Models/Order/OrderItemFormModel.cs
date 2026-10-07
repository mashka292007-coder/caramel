using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.Order
{
    public class OrderItemFormModel
    {
        public int? ProductId { get; set; }

        [Range(1, 9999, ErrorMessage = "Кількість від 1 до 9999")]
        public int Quantity { get; set; } = 1;
        public string? ProductName { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
