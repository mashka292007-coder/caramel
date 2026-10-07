namespace CaramelCloud.UI.Models.Order
{
    public class OrderListItemViewModel
    {
        public int Id { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public bool IsClientOrder { get; set; }
        public string Source { get; set; } = "admin";
    }
}
