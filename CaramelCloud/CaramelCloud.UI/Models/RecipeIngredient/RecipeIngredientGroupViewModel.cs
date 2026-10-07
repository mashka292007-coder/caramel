namespace CaramelCloud.UI.Models.RecipeIngredient
{
    public class RecipeIngredientGroupViewModel
    {
        public int RecipeId { get; set; }
        public string RecipeDescription { get; set; } = "—";
        public List<RecipeIngredientViewModel> Ingredients { get; set; } = new();
    }
}
