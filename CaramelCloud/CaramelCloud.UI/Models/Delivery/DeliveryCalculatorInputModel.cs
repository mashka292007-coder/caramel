using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.Delivery
{
    public class DeliveryCalculatorInputModel
    {
        [Required(ErrorMessage = "Введіть вашу адресу")]
        [DisplayName("Ваша адреса доставки")]
        [MinLength(5, ErrorMessage = "Адреса занадто коротка")]
        public string Address { get; set; } = string.Empty;
    }
}
