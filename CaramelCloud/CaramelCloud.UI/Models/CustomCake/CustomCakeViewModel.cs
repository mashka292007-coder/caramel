namespace CaramelCloud.UI.Models.CustomCake
{
    public class CustomCakeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int LayerCount { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<CakeLayerViewModel> Layers { get; set; } = new();
    }
}
