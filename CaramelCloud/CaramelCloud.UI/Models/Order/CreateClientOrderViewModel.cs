using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.Order
{
    public class CreateClientOrderViewModel
    {
        [DisplayName("Спосіб оплати")]
        [Required(ErrorMessage = "Оберіть спосіб оплати")]
        public int? PaymentMethodId { get; set; }

        [DisplayName("Спосіб доставки")]
        [Required(ErrorMessage = "Оберіть спосіб доставки")]
        public int? DeliveryMethodId { get; set; }

        [DisplayName("Адреса доставки")]
        public int? DeliveryAddressId { get; set; }

        [DisplayName("Адреса (якщо нова)")]
        public string? DeliveryAddressCustom { get; set; }

        [DisplayName("Вартість доставки")]
        public decimal DeliveryCost { get; set; } = 0;

        [DisplayName("Бажана дата отримання")]
        [DataType(DataType.DateTime)]
        public DateTime? DeliveryDate { get; set; }

        [DisplayName("Коментар до замовлення")]
        [MaxLength(500)]
        public string? Comment { get; set; }

        public List<OrderItemFormModel> Items { get; set; } = new() { new() };
        public IEnumerable<SelectListItem> Products { get; set; } = [];
        public IEnumerable<SelectListItem> PaymentMethods { get; set; } = [];
        public IEnumerable<SelectListItem> DeliveryMethods { get; set; } = [];
        public IEnumerable<SelectListItem> DeliveryAddresses { get; set; } = [];

        public Dictionary<string, string> ProductImages { get; set; } = new();
    }
}
