namespace CaramelCloud.UI.Models.CustomCake
{
    public class AiAdviceRequest
    {
        public List<LayerInfo> Layers { get; set; } = new();
        public List<string> AvailableSponges { get; set; } = new();
        public List<string> AvailableFills { get; set; } = new();
    }
}
