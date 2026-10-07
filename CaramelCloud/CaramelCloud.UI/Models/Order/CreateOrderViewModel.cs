using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.Order
{
    public class CreateOrderViewModel
    {
        [DisplayName("Профіль клієнта (якщо зареєстрований)")]
        public int? CustomerProfileId { get; set; }

        [DisplayName("Ім'я клієнта")]
        [MaxLength(100)]
        public string? CustomerName { get; set; }

        [DisplayName("Телефон клієнта")]
        [MaxLength(20)]
        public string? CustomerPhone { get; set; }

        [DisplayName("Спосіб оплати")]
        public int? PaymentMethodId { get; set; }

        [DisplayName("Спосіб доставки")]
        public int? DeliveryMethodId { get; set; }

        [DisplayName("Адреса доставки")]
        public int? DeliveryAddressId { get; set; }

        [DisplayName("Дата доставки")]
        [DataType(DataType.DateTime)]
        public DateTime? DeliveryDate { get; set; }

        [DisplayName("Коментар")]
        [MaxLength(500)]
        public string? Comment { get; set; }

        public List<OrderItemFormModel> Items { get; set; } = new() { new() };
        public IEnumerable<SelectListItem> CustomerProfiles { get; set; } = [];
        public IEnumerable<SelectListItem> Products { get; set; } = [];
        public IEnumerable<SelectListItem> PaymentMethods { get; set; } = [];
        public IEnumerable<SelectListItem> DeliveryMethods { get; set; } = [];
        public IEnumerable<SelectListItem> DeliveryAddresses { get; set; } = [];
        public Dictionary<string, string> ProductImages { get; set; } = new();
    }
}
