using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CaramelCloud.UI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CakeLayerTypeController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IImageService _imageService;

        public CakeLayerTypeController(IUnitOfWork uow, IImageService imageService)
        {
            _uow = uow;
            _imageService = imageService;
        }

        public IActionResult Index()
        {
            var items = _uow.CakeLayerType.GetAll();
            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            PopulateRecipes();
            return View(new CakeLayerType());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CakeLayerType model, IFormFile? image)
        {
            ModelState.Remove(nameof(model.ImageUrl));
            ModelState.Remove(nameof(model.Recipe));
            ModelState.Remove(nameof(model.CustomCakeLayers));

            if (!ModelState.IsValid)
            {
                PopulateRecipes();
                return View(model);
            }

            if (image is { Length: > 0 })
                model.ImageUrl = await _imageService.UploadImageAsync(image, "layers");

            _uow.CakeLayerType.Add(model);
            _uow.Save();

            TempData["Success"] = "Тип коржа додано";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var item = _uow.CakeLayerType.Get(x => x.Id == id);
            if (item is null) return NotFound();
            PopulateRecipes();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CakeLayerType model, IFormFile? image, bool removeImage = false)
        {
            ModelState.Remove(nameof(model.ImageUrl));
            ModelState.Remove(nameof(model.Recipe));
            ModelState.Remove(nameof(model.CustomCakeLayers));

            if (!ModelState.IsValid)
            {
                PopulateRecipes();
                return View(model);
            }

            var existing = _uow.CakeLayerType.Get(x => x.Id == model.Id, tracked: true);
            if (existing is null) return NotFound();

            existing.Name = model.Name;
            existing.ExtraPrice = model.ExtraPrice;
            existing.Description = model.Description;
            existing.IsAvailable = model.IsAvailable;
            existing.RecipeId = model.RecipeId;

            if (removeImage)
                existing.ImageUrl = null;
            else if (image is { Length: > 0 })
                existing.ImageUrl = await _imageService.UploadImageAsync(image, "layers");

            _uow.Save();
            TempData["Success"] = "Тип коржа оновлено";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var item = _uow.CakeLayerType.Get(x => x.Id == id, tracked: true);
            if (item is null) return NotFound();

            var inUse = _uow.CustomCakeLayer.GetAll(l => l.LayerTypeId == id).Any();
            if (inUse)
            {
                TempData["ErrorMessage"] = "Цей тип коржа вже використовується в тортах — видалити неможливо";
                return RedirectToAction(nameof(Index));
            }

            _uow.CakeLayerType.Remove(item);
            _uow.Save();
            TempData["Success"] = "Тип коржа видалено";
            return RedirectToAction(nameof(Index));
        }

        private void PopulateRecipes()
        {
            ViewBag.Recipes = _uow.Recipe.GetAll()
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(r.Description)
                            ? $"Рецепт #{r.Id}"
                            : $"Рецепт #{r.Id} ({r.Description})"
                })
                .ToList();
        }
    }
}
