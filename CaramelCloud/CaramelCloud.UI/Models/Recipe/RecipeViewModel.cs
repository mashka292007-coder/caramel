namespace CaramelCloud.UI.Models.Recipe
{
    public class RecipeViewModel : CreateRecipeViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = "—";
        public int IngredientCount { get; set; }
    }
}
