namespace CaramelCloud.UI.Models.Order
{
    public class OrderViewModel
    {
        public int Id { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? DeliveryMethod { get; set; }
        public string? DeliveryAddress { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Comment { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByUser { get; set; } = string.Empty;
        public bool IsClientOrder { get; set; }
        public List<OrderItemViewModel> Items { get; set; } = [];
    }

}
