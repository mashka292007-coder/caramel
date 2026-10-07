namespace CaramelCloud.UI.Models.CustomCake
{
    public class CakeLayerViewModel
    {
        public int LayerOrder { get; set; }
        public string LayerName { get; set; } = string.Empty;
        public string? LayerImage { get; set; }
        public string FillingName { get; set; } = string.Empty;
        public string? FillingImage { get; set; }
        public decimal ExtraPrice { get; set; }
    }
}
