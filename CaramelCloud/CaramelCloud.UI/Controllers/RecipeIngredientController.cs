using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.UI.Models.RecipeIngredient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CaramelCloud.UI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RecipeIngredientController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public RecipeIngredientController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index(int? recipeId)
        {
            var ingredients = _unitOfWork.RecipeIngredient
                .GetAll(
                    filter: recipeId.HasValue ? i => i.RecipeId == recipeId.Value : null,
                    includeProperties: "Recipe,InventoryItem")
                .ToList();

            ViewBag.FilteredRecipeId = recipeId;
            ViewBag.FilteredRecipeDescription = recipeId.HasValue
                ? (ingredients.FirstOrDefault()?.Recipe?.Description is { Length: > 0 } d
                    ? d
                    : $"Рецепт #{recipeId}")
                : null;

            var groups = ingredients
                .GroupBy(i => i.RecipeId)
                .Select(g => new RecipeIngredientGroupViewModel
                {
                    RecipeId = g.Key,
                    RecipeDescription = g.First().Recipe?.Description is { Length: > 0 } d
                                        ? d
                                        : $"Рецепт #{g.Key}",
                    Ingredients = g.Select(i => new RecipeIngredientViewModel
                    {
                        Id = i.Id,
                        RecipeId = i.RecipeId,
                        RecipeDescription = i.Recipe?.Description ?? $"Рецепт #{i.RecipeId}",
                        InventoryItemId = i.InventoryItemId,
                        InventoryItemName = i.InventoryItem?.ItemName ?? "—",
                        Unit = i.InventoryItem?.Unit ?? string.Empty,
                        Quantity = i.Quantity
                    }).ToList()
                })
                .OrderBy(g => g.RecipeDescription)
                .ToList();

            return View(groups);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new CreateRecipeIngredientViewModel();
            PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRecipeIngredientViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model);
                return View(model);
            }

            var ingredient = new RecipeIngredient
            {
                RecipeId = model.RecipeId,
                InventoryItemId = model.InventoryItemId,
                Quantity = model.Quantity
            };

            _unitOfWork.RecipeIngredient.Add(ingredient);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Інгредієнт успішно додано";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var ingredient = _unitOfWork.RecipeIngredient.Get(x => x.Id == id.Value);
            if (ingredient is null) return NotFound();

            var model = new RecipeIngredientViewModel
            {
                Id = ingredient.Id,
                RecipeId = ingredient.RecipeId,
                InventoryItemId = ingredient.InventoryItemId,
                Quantity = ingredient.Quantity
            };

            PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RecipeIngredientViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model);
                return View(model);
            }

            var ingredientFromDb = _unitOfWork.RecipeIngredient.Get(x => x.Id == model.Id);
            if (ingredientFromDb is null) return NotFound();

            ingredientFromDb.RecipeId = model.RecipeId;
            ingredientFromDb.InventoryItemId = model.InventoryItemId;
            ingredientFromDb.Quantity = model.Quantity;

            _unitOfWork.RecipeIngredient.Update(ingredientFromDb);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Інгредієнт успішно оновлено";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var ingredient = _unitOfWork.RecipeIngredient.Get(
                x => x.Id == id.Value,
                includeProperties: "Recipe,InventoryItem");

            if (ingredient is null) return NotFound();

            var model = new RecipeIngredientViewModel
            {
                Id = ingredient.Id,
                RecipeId = ingredient.RecipeId,
                RecipeDescription = ingredient.Recipe?.Description is { Length: > 0 } d
                                    ? d
                                    : $"Рецепт #{ingredient.RecipeId}",
                InventoryItemId = ingredient.InventoryItemId,
                InventoryItemName = ingredient.InventoryItem?.ItemName ?? "—",
                Unit = ingredient.InventoryItem?.Unit ?? string.Empty,
                Quantity = ingredient.Quantity
            };

            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var ingredient = _unitOfWork.RecipeIngredient.Get(x => x.Id == id.Value);
            if (ingredient is null) return NotFound();

            _unitOfWork.RecipeIngredient.Remove(ingredient);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Інгредієнт успішно видалено";
            return RedirectToAction(nameof(Index));
        }

        private void PopulateDropdowns(CreateRecipeIngredientViewModel model)
        {
            model.Recipes = _unitOfWork.Recipe
                .GetAll()
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = string.IsNullOrEmpty(r.Description)
                            ? $"Рецепт #{r.Id}"
                            : r.Description
                })
                .ToList();

            model.InventoryItems = _unitOfWork.InventoryItem
                .GetAll()
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = $"{i.ItemName} ({i.Unit})"
                })
                .ToList();
        }
    }
}
