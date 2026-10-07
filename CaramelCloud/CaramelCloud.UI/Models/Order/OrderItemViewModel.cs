namespace CaramelCloud.UI.Models.Order
{
    public class OrderItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total => UnitPrice * Quantity;
    }

}
