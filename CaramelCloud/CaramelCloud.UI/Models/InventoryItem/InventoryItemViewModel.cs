namespace CaramelCloud.UI.Models.InventoryItem
{
    public class InventoryItemViewModel : CreateInventoryItemViewModel
    {
        public int Id { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsCritical => Quantity < MinQuantity;
    }
}
