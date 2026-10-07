namespace CaramelCloud.UI.Models.InventoryPurchase
{
    public class InventoryPurchaseViewModel
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal TotalCost { get; set; }
        public string? Supplier { get; set; }
        public DateTime PurchaseDate { get; set; }
    }
}
