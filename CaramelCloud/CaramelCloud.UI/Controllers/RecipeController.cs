using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.UI.Models.Recipe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaramelCloud.UI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RecipeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public RecipeController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var recipes = _unitOfWork.Recipe
                .GetAll(includeProperties: "Product,RecipeIngredients")
                .Select(r => new RecipeViewModel
                {
                    Id = r.Id,
                    Description = r.Description,
                    Notes = r.Notes,
                    ProductName = r.Product?.Name ?? "—",
                    IngredientCount = r.RecipeIngredients?.Count ?? 0
                })
                .ToList();

            return View(recipes);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateRecipeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRecipeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var recipe = new Recipe
            {
                Description = model.Description?.Trim(),
                Notes = model.Notes?.Trim()
            };

            _unitOfWork.Recipe.Add(recipe);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Рецепт успішно створено";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var recipe = _unitOfWork.Recipe.Get(x => x.Id == id.Value);
            if (recipe is null) return NotFound();

            var model = new RecipeViewModel
            {
                Id = recipe.Id,
                Description = recipe.Description,
                Notes = recipe.Notes
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RecipeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var recipeFromDb = _unitOfWork.Recipe.Get(x => x.Id == model.Id);
            if (recipeFromDb is null) return NotFound();

            recipeFromDb.Description = model.Description?.Trim();
            recipeFromDb.Notes = model.Notes?.Trim();

            _unitOfWork.Recipe.Update(recipeFromDb);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Рецепт успішно оновлено";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var recipe = _unitOfWork.Recipe.Get(
                x => x.Id == id.Value,
                includeProperties: "Product,RecipeIngredients");

            if (recipe is null) return NotFound();

            var model = new RecipeViewModel
            {
                Id = recipe.Id,
                Description = recipe.Description,
                Notes = recipe.Notes,
                ProductName = recipe.Product?.Name ?? "—",
                IngredientCount = recipe.RecipeIngredients?.Count ?? 0
            };

            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var recipe = _unitOfWork.Recipe.Get(x => x.Id == id.Value);
            if (recipe is null) return NotFound();

            _unitOfWork.Recipe.Remove(recipe);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Рецепт успішно видалено";
            return RedirectToAction(nameof(Index));
        }
    }
}
