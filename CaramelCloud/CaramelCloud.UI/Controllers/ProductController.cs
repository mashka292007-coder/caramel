using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.Services.ExternalApi.Impl;
using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using CaramelCloud.UI.Models.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CaramelCloud.UI.Controllers
{
    [Authorize(Roles = "Admin,Customer")]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageService _imageService;
        private readonly GeminiService _gemini;

        public ProductController(IUnitOfWork unitOfWork, IImageService imageService, GeminiService gemini)
        {
            _unitOfWork = unitOfWork;
            _imageService = imageService;
            _gemini = gemini;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            var products = _unitOfWork.Product
                .GetAll(includeProperties: "Category,Recipe")
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category?.Name ?? "—",
                    RecipeId = p.RecipeId,
                    RecipeName = p.Recipe?.Description ?? "—",
                    Weight = p.Weight,
                    ServingSize = p.ServingSize,
                    IsAvailable = p.IsAvailable
                })
                .ToList();

            return View(products);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            var model = new CreateProductViewModel();
            PopulateDropdowns(model);
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model);
                return View(model);
            }

            var product = new Product
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                Price = model.Price,
                CategoryId = model.CategoryId,
                RecipeId = model.RecipeId,
                Weight = model.Weight,
                ServingSize = model.ServingSize,
                IsAvailable = model.IsAvailable
            };

            if (model.ImageFile != null && model.ImageFile.Length > 0)
                product.ImageUrl = await _imageService.UploadImageAsync(model.ImageFile, "Products");

            _unitOfWork.Product.Add(product);
            _unitOfWork.Save();

            TempData["success"] = "Продукт успішно створено";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]

        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var product = _unitOfWork.Product.Get(x => x.Id == id.Value);
            if (product is null) return NotFound();

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                CategoryId = product.CategoryId,
                RecipeId = product.RecipeId,
                Weight = product.Weight,
                ServingSize = product.ServingSize,
                IsAvailable = product.IsAvailable
            };

            PopulateDropdowns(model);
            return View(model);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model);
                return View(model);
            }

            var productFromDb = _unitOfWork.Product.Get(x => x.Id == model.Id);
            if (productFromDb is null) return NotFound();

            productFromDb.Name = model.Name.Trim();
            productFromDb.Description = model.Description?.Trim();
            productFromDb.Price = model.Price;
            productFromDb.CategoryId = model.CategoryId;
            productFromDb.RecipeId = model.RecipeId;
            productFromDb.Weight = model.Weight;
            productFromDb.ServingSize = model.ServingSize;
            productFromDb.IsAvailable = model.IsAvailable;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(productFromDb.ImageUrl))
                    await _imageService.DeleteImageAsync(productFromDb.ImageUrl);

                productFromDb.ImageUrl = await _imageService.UploadImageAsync(model.ImageFile, "Products");
            }

            _unitOfWork.Product.Update(productFromDb);
            _unitOfWork.Save();

            TempData["success"] = "Продукт успішно оновлено";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var product = _unitOfWork.Product.Get(
                x => x.Id == id.Value,
                includeProperties: "Category,Recipe");

            if (product is null) return NotFound();

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? "—",
                RecipeId = product.RecipeId,
                RecipeName = product.Recipe?.Description ?? "—",
                Weight = product.Weight,
                ServingSize = product.ServingSize,
                IsAvailable = product.IsAvailable
            };

            return View(model);
        }

        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var product = _unitOfWork.Product.Get(x => x.Id == id.Value);
            if (product is null) return NotFound();

            if (!string.IsNullOrEmpty(product.ImageUrl))
                await _imageService.DeleteImageAsync(product.ImageUrl);

            _unitOfWork.Product.Remove(product);
            _unitOfWork.Save();

            TempData["success"] = "Продукт успішно видалено";
            return RedirectToAction(nameof(Index));
        }
        private void PopulateDropdowns(CreateProductViewModel model)
        {
            model.Categories = _unitOfWork.Category
                .GetAll()
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToList();

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
        }

        public record ChatMessage(string Role, string Text);
        public record ChatRequest(string Message, List<ChatMessage>? History = null);

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            var products = _unitOfWork.Product
                .GetAll(p => p.IsAvailable, includeProperties: "Category")
                .Select(p => $"- {p.Name} ({p.Category?.Name ?? "без категорії"}): {p.Description ?? "без опису"}, ціна {p.Price:N0} ₴")
                .ToList();

            var menu = string.Join("\n", products);

            var historyText = "";
            if (request.History?.Any() == true)
            {
                historyText = "\nІсторія розмови:\n" + string.Join("\n",
                    request.History.Select(h => $"{(h.Role == "user" ? "Клієнт" : "Консультант")}: {h.Text}"));
            }

            var prompt = $"""
            Ти консультант кондитерської CaramelCloud.
            Відповідай українською, коротко і доброзичливо (2-4 речення).
            Ось наше поточне меню:
            {menu}
            {historyText}

            Клієнт пише: {request.Message}

            Якщо клієнт каже що вже обрав, готовий замовити, або дякує — дай коротку відповідь
            і більше нічого не пропонуй.
            Якщо клієнт шукає щось конкретне — порадь позиції з меню вище.
            Якщо питання не стосується меню — ввічливо поверни до теми десертів.

            Якщо ти рекомендуєш конкретні позиції з меню — ОБОВ'ЯЗКОВО в самому кінці відповіді,
            на ОКРЕМОМУ новому рядку напиши ТІЛЬКИ:
            PRODUCTS: Назва продукту 1, Назва продукту 2
            Якщо конкретних рекомендацій немає — не додавай цей рядок взагалі.
            """;

            var reply = await _gemini.AskAsync(prompt);

            var productsMatch = System.Text.RegularExpressions.Regex.Match(
                reply, @"PRODUCTS:\s*(.+?)(?:\n|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            List<object>? recommended = null;
            if (productsMatch.Success)
            {
                var names = productsMatch.Groups[1].Value.Split(',').Select(n => n.Trim()).ToList();
                recommended = _unitOfWork.Product
                    .GetAll(p => p.IsAvailable && names.Any(n =>
                        p.Name.ToLower().Contains(n.ToLower()) || n.ToLower().Contains(p.Name.ToLower())),
                        includeProperties: "Category")
                    .Select(p => (object)new { p.Id, p.Name, p.Price, p.ImageUrl })
                    .ToList();
            }

            var cleanReply = System.Text.RegularExpressions.Regex.Replace(
                reply, @"\s*PRODUCTS:\s*.+?(?:\n|$)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

            return Json(new { reply = cleanReply, products = recommended });
        }
    }
}
