using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.CustomCake
{
    public class PlaceOrderWithCakeViewModel
    {
        public int CustomCakeId { get; set; }
        public string CakeName { get; set; } = string.Empty;
        public decimal CakePrice { get; set; }

        public int? DeliveryMethodId { get; set; }
        public int? DeliveryAddressId { get; set; }
        public decimal DeliveryCost { get; set; } = 0;
        public string? DeliveryAddressCustom { get; set; }
        public int? PaymentMethodId { get; set; }
        public DateTime? DeliveryDate { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        public List<SelectListItem> DeliveryMethods { get; set; } = new();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Addresses { get; set; } = new();
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> PaymentMethods { get; set; } = new();

    }
}
