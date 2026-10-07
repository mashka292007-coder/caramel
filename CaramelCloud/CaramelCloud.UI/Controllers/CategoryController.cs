using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using CaramelCloud.UI.Models.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaramelCloud.UI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageService _imageService;

        public CategoryController(IUnitOfWork unitOfWork, IImageService imageService)
        {
            _unitOfWork = unitOfWork;
            _imageService = imageService;
        }

        public IActionResult Index()
        {
            var categories = _unitOfWork.Category.GetAll().Select(c => new CategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl
            }).ToList();

            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateCategoryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var category = new Category
            {
                Name = model.Name.Trim(),
                Description = model.Description
            };

            if (model.ImageFile != null)
            {
                category.ImageUrl = await _imageService.UploadImageAsync(model.ImageFile, "Categories");
            }

            _unitOfWork.Category.Add(category);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var category = _unitOfWork.Category.Get(x => x.Id == id.Value);
            if (category is null) return NotFound();

            var model = new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var categoryFromDb = _unitOfWork.Category.Get(x => x.Id == model.Id);
            if (categoryFromDb is null) return NotFound();

            categoryFromDb.Name = model.Name.Trim();
            categoryFromDb.Description = model.Description;

            if (model.ImageFile != null)
            {
                if (!string.IsNullOrEmpty(categoryFromDb.ImageUrl))
                {
                    await _imageService.DeleteImageAsync(categoryFromDb.ImageUrl);
                }
                categoryFromDb.ImageUrl = await _imageService.UploadImageAsync(model.ImageFile, "Categories");
            }

            _unitOfWork.Category.Update(categoryFromDb);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var category = _unitOfWork.Category.Get(x => x.Id == id.Value);
            if (category is null) return NotFound();

            var model = new CategoryViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                ImageUrl = category.ImageUrl
            };

            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var category = _unitOfWork.Category.Get(x => x.Id == id.Value);
            if (category is null) return NotFound();

            if (!string.IsNullOrEmpty(category.ImageUrl))
            {
                await _imageService.DeleteImageAsync(category.ImageUrl);
            }

            _unitOfWork.Category.Remove(category);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }
    }
}
