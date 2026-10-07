namespace CaramelCloud.UI.Models.Api
{
    public class UpdateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public int? RecipeId { get; set; }
        public decimal? Weight { get; set; }
        public int? ServingSize { get; set; }
        public bool IsAvailable { get; set; }
    }
}
