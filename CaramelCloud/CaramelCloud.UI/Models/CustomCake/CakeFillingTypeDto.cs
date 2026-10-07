namespace CaramelCloud.UI.Models.CustomCake
{
    public class CakeFillingTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal ExtraPrice { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }
}
