using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.Order
{
    public class EditOrderViewModel
    {
        public int Id { get; set; }

        [DisplayName("Ім'я клієнта")]
        [MaxLength(100)]
        public string? CustomerName { get; set; }

        [DisplayName("Телефон клієнта")]
        [MaxLength(20)]
        public string? CustomerPhone { get; set; }

        [DisplayName("Статус")]
        [Required(ErrorMessage = "Оберіть статус")]
        public int StatusId { get; set; }

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

        public bool IsClientOrder { get; set; }

        public IEnumerable<SelectListItem> Statuses { get; set; } = [];
        public IEnumerable<SelectListItem> PaymentMethods { get; set; } = [];
        public IEnumerable<SelectListItem> DeliveryMethods { get; set; } = [];
        public IEnumerable<SelectListItem> DeliveryAddresses { get; set; } = [];
    }

}
