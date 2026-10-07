using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using CaramelCloud.Infrastructure.Services.ExternalApi.Impl;
using CaramelCloud.Infrastructure.Services.Interfaces;
using CaramelCloud.UI.Models.CustomCake;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CaramelCloud.UI.Controllers
{
    [Authorize]
    public class CustomCakeController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly ICustomCakeService _cakeService;
        private readonly UserManager<User> _userManager;
        private readonly GeminiService _gemini;

        public CustomCakeController(
            IUnitOfWork uow,
            ICustomCakeService cakeService,
            UserManager<User> userManager,
            GeminiService gemini)
        {
            _uow = uow;
            _cakeService = cakeService;
            _userManager = userManager;
            _gemini = gemini;
        }

        [HttpGet]
        public IActionResult Build()
        {
            var model = new BuildCakeViewModel
            {
                Sizes = new List<CakeSizeOption>
                {
                    new() { Label = "1 кг",   LayerCount = 2, BasePrice = 150 },
                    new() { Label = "1.5 кг", LayerCount = 3, BasePrice = 200 },
                    new() { Label = "2 кг",   LayerCount = 4, BasePrice = 260 },
                },
                LayerTypes = _uow.CakeLayerType
                    .GetAll(lt => lt.IsAvailable)
                    .Select(lt => new CakeLayerTypeDto
                    {
                        Id = lt.Id,
                        Name = lt.Name,
                        ExtraPrice = lt.ExtraPrice,
                        ImageUrl = lt.ImageUrl,
                        Description = lt.Description,
                    }).ToList(),

                FillingTypes = _uow.CakeFillingType
                    .GetAll(ft => ft.IsAvailable)
                    .Select(ft => new CakeFillingTypeDto
                    {
                        Id = ft.Id,
                        Name = ft.Name,
                        ExtraPrice = ft.ExtraPrice,
                        ImageUrl = ft.ImageUrl,
                        Description = ft.Description,
                    }).ToList(),
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Build(BuildCakeFormModel form)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Build));

            if (form.Layers == null || form.Layers.Count == 0)
            {
                TempData["ErrorMessage"] = "Додайте хоча б один шар";
                return RedirectToAction(nameof(Build));
            }

            var userId = GetUserId();

            var cake = new CustomCake
            {
                UserId = userId,
                Name = string.IsNullOrWhiteSpace(form.Name) ? "Мій торт" : form.Name.Trim(),
                TotalPrice = form.BasePrice,
            };

            var layers = form.Layers.Select(l => new CakeLayerInput(
                l.LayerOrder,
                l.LayerTypeId,
                l.FillingTypeId)).ToList();

            var cakeId = await _cakeService.BuildCakeAsync(cake, layers);

            TempData["Success"] = "Торт збережено! Оформіть замовлення нижче.";
            return RedirectToAction(nameof(Order), new { cakeId });
        }

        [HttpPost]
        public async Task<IActionResult> GetAiAdvice([FromBody] AiAdviceRequest request)
        {
            var layers = request.Layers
                .Select(l => $"Шар {l.Order}: корж «{l.SpongeName}», начинка «{l.FillingName}»")
                .ToList();

            var availableSponges = string.Join(", ", request.AvailableSponges);
            var availableFills = string.Join(", ", request.AvailableFills);

            var prompt = $"""
            Ти консультант кондитерської CaramelCloud. 
            Клієнт складає торт з таких шарів:
            {string.Join("\n", layers)}
    
            Доступні коржі: {availableSponges}
            Доступні начинки: {availableFills}
    
            Коротко (2-3 речення українською) скажи чи це вдале поєднання смаків, 
            що добре а що можна покращити. Якщо є кращі варіанти — порадь конкретно з наведеного списку.
            Будь доброзичливим і конкретним.
            """;

            var advice = await _gemini.AskAsync(prompt);
            return Json(new { advice });
        }

        [HttpGet]
        public IActionResult Order(int cakeId)
        {
            var cake = _uow.CustomCake.GetWithLayers(cakeId);

            if (cake is null) return NotFound();
            if (cake.UserId != GetUserId() && !User.IsInRole("Admin")) return Forbid();

            var userId = GetUserId();

            var model = new PlaceOrderWithCakeViewModel
            {
                CustomCakeId = cake.Id,
                CakeName = cake.Name ?? "Мій торт",
                CakePrice = cake.TotalPrice,

                Addresses = _uow.DeliveryAddress
                    .GetAll(a => a.UserId == userId)
                    .OrderByDescending(a => a.IsDefault)
                    .Select(a => new SelectListItem
                    {
                        Value = a.Id.ToString(),
                        Text = $"{a.City}, {a.AddressLine}",
                        Selected = a.IsDefault,
                    }).ToList(),

                PaymentMethods = _uow.PaymentMethod
                    .GetAll()
                    .Select(m => new SelectListItem
                    {
                        Value = m.Id.ToString(),
                        Text = m.Name,
                    }).ToList(),

                DeliveryMethods = _uow.DeliveryMethod
                    .GetAll()
                    .Select(m => new SelectListItem
                    {
                        Value = m.Id.ToString(),
                        Text = m.Name,
                    }).ToList(),
            };

            ViewBag.Layers = cake.CustomCakeLayers
                .OrderBy(l => l.LayerOrder)
                .Select(l => new CakeLayerViewModel
                {
                    LayerOrder = l.LayerOrder,
                    LayerName = l.LayerType?.Name ?? "—",
                    LayerImage = l.LayerType?.ImageUrl,
                    FillingName = l.FillingType?.Name ?? "—",
                    FillingImage = l.FillingType?.ImageUrl,
                    ExtraPrice = (l.LayerType?.ExtraPrice ?? 0) + (l.FillingType?.ExtraPrice ?? 0),
                }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Order(PlaceOrderWithCakeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var cake = _uow.CustomCake.GetWithLayers(model.CustomCakeId);

                var userId = GetUserId();

                model.Addresses = _uow.DeliveryAddress
                    .GetAll(a => a.UserId == userId)
                    .Select(a => new SelectListItem
                    {
                        Value = a.Id.ToString(),
                        Text = $"{a.City}, {a.AddressLine}"
                    }).ToList();

                model.PaymentMethods = _uow.PaymentMethod
                    .GetAll()
                    .Select(m => new SelectListItem
                    {
                        Value = m.Id.ToString(),
                        Text = m.Name
                    }).ToList();

                model.DeliveryMethods = _uow.DeliveryMethod
                    .GetAll()
                    .Select(m => new SelectListItem
                    {
                        Value = m.Id.ToString(),
                        Text = m.Name
                    }).ToList();

                if (cake is not null)
                {
                    ViewBag.Layers = cake.CustomCakeLayers
                        .OrderBy(l => l.LayerOrder)
                        .Select(l => new CakeLayerViewModel
                        {
                            LayerOrder = l.LayerOrder,
                            LayerName = l.LayerType?.Name ?? "—",
                            LayerImage = l.LayerType?.ImageUrl,
                            FillingName = l.FillingType?.Name ?? "—",
                            FillingImage = l.FillingType?.ImageUrl,
                            ExtraPrice = (l.LayerType?.ExtraPrice ?? 0) + (l.FillingType?.ExtraPrice ?? 0),
                        }).ToList();
                }

                return View(model);
            }
            var userId2 = GetUserId();
            int? finalAddressId = model.DeliveryAddressId;

            if (model.DeliveryMethodId == 2 && finalAddressId == null
                && !string.IsNullOrWhiteSpace(model.DeliveryAddressCustom))
            {
                var normalized = model.DeliveryAddressCustom.Trim();
                var existing = _uow.DeliveryAddress
                    .Get(a => a.UserId == userId2 &&
                            a.AddressLine.ToLower() == normalized.ToLower());
                if (existing != null)
                {
                    finalAddressId = existing.Id;
                }
                else
                {
                    var newAddr = new DeliveryAddress
                    {
                        UserId = userId2,
                        City = "",
                        AddressLine = normalized,
                        IsDefault = false
                    };
                    _uow.DeliveryAddress.Add(newAddr);
                    await _uow.SaveAsync();
                    finalAddressId = newAddr.Id;
                }
            }
            var orderId = await _cakeService.PlaceOrderWithCakeAsync(
                userId: userId2,
                customCakeId: model.CustomCakeId,
                deliveryAddressId: finalAddressId,
                deliveryMethodId: model.DeliveryMethodId,
                paymentMethodId: model.PaymentMethodId,
                deliveryDate: model.DeliveryDate,
                comment: model.Comment,
                deliveryCost: model.DeliveryCost);

            TempData["Success"] = "Замовлення оформлено! Очікуйте підтвердження.";
            return RedirectToAction("Details", "Order", new { id = orderId });
        }

        [HttpGet]
        public IActionResult MyCakes()
        {
            var userId = GetUserId();
            var cakes = _uow.CustomCake.GetByUser(userId);

            var model = cakes.Select(c => new CustomCakeViewModel
            {
                Id = c.Id,
                Name = c.Name ?? "Без назви",
                LayerCount = c.LayerCount,
                TotalPrice = c.TotalPrice,
                CreatedAt = c.CreatedAt,
                Layers = c.CustomCakeLayers
                    .OrderBy(l => l.LayerOrder)
                    .Select(l => new CakeLayerViewModel
                    {
                        LayerOrder = l.LayerOrder,
                        LayerName = l.LayerType?.Name ?? "—",
                        LayerImage = l.LayerType?.ImageUrl,
                        FillingName = l.FillingType?.Name ?? "—",
                        FillingImage = l.FillingType?.ImageUrl,
                        ExtraPrice = (l.LayerType?.ExtraPrice ?? 0) + (l.FillingType?.ExtraPrice ?? 0),
                    }).ToList(),
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var cake = _uow.CustomCake.Get(c => c.Id == id);
            if (cake is null) return NotFound();
            if (cake.UserId != GetUserId() && !User.IsInRole("Admin")) return Forbid();

            try
            {
                _cakeService.DeleteCake(id);
                TempData["Success"] = "Торт видалено";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(MyCakes));
        }

        [HttpGet]
        public IActionResult GetPrice(decimal basePrice, [FromQuery] int[] layerTypeIds, [FromQuery] int[] fillingTypeIds)
        {
            var layers = layerTypeIds.Zip(fillingTypeIds, (l, f) => new CakeLayerInput(0, l, f)).ToList();
            var total = _cakeService.CalculatePrice(basePrice, layers);
            return Json(new { total });
        }

        private int GetUserId() => int.Parse(_userManager.GetUserId(User)!);
    }
}
