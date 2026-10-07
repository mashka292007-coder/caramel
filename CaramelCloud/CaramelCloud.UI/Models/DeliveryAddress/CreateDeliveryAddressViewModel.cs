using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.DeliveryAddress
{
    public class CreateDeliveryAddressViewModel
    {
        [DisplayName("Місто")]
        [Required(ErrorMessage = "Вкажіть місто")]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [DisplayName("Адреса (вулиця, будинок, квартира)")]
        [Required(ErrorMessage = "Вкажіть адресу")]
        [MaxLength(250)]
        public string AddressLine { get; set; } = string.Empty;

        [DisplayName("Поштовий індекс")]
        [MaxLength(20)]
        public string? PostalCode { get; set; }

        [DisplayName("Зробити адресою за замовчуванням")]
        public bool IsDefault { get; set; } = false;
    }
}
