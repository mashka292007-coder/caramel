namespace CaramelCloud.UI.Models.RecipeIngredient
{
    public class RecipeIngredientViewModel : CreateRecipeIngredientViewModel
    {
        public int Id { get; set; }
        public string RecipeDescription { get; set; } = "—";
        public string InventoryItemName { get; set; } = "—";
        public string Unit { get; set; } = string.Empty;
    }
}
