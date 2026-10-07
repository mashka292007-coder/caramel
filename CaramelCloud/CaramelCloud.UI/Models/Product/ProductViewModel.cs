namespace CaramelCloud.UI.Models.Product
{
    public class ProductViewModel : CreateProductViewModel
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
    }
}
