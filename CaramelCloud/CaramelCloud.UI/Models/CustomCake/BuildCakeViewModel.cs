namespace CaramelCloud.UI.Models.CustomCake
{
    public class BuildCakeViewModel
    {
        public List<CakeSizeOption> Sizes { get; set; } = new();
        public List<CakeLayerTypeDto> LayerTypes { get; set; } = new();
        public List<CakeFillingTypeDto> FillingTypes { get; set; } = new();
    }
}
