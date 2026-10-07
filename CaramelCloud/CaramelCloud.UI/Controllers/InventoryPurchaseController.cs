using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.Services.Interfaces;
using CaramelCloud.UI.Models.InventoryPurchase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CaramelCloud.UI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InventoryPurchaseController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IInventoryPurchaseService _service;

        public InventoryPurchaseController(IUnitOfWork uow, IInventoryPurchaseService service)
        {
            _uow = uow;
            _service = service;
        }

        public IActionResult Index()
        {
            var purchases = _uow.InventoryPurchase
                .GetAll(includeProperties: "InventoryItem")
                .OrderByDescending(p => p.PurchaseDate)
                .Select(p => new InventoryPurchaseViewModel
                {
                    Id = p.Id,
                    ItemName = p.InventoryItem?.ItemName ?? "—",
                    Unit = p.InventoryItem?.Unit ?? "",
                    Quantity = p.Quantity,
                    PricePerUnit = p.PricePerUnit,
                    TotalCost = p.TotalCost,
                    Supplier = p.Supplier,
                    PurchaseDate = p.PurchaseDate,
                })
                .ToList();

            ViewBag.LowStockWarnings = _service.GetLowStockWarnings().ToList();
            return View(purchases);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new CreateInventoryPurchaseViewModel();
            PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateInventoryPurchaseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model);
                return View(model);
            }

            var purchase = new InventoryPurchase
            {
                InventoryItemId = model.InventoryItemId,
                Quantity = model.Quantity,
                PricePerUnit = model.PricePerUnit,
                Supplier = model.Supplier?.Trim(),
            };

            _service.AddPurchase(purchase);
            TempData["Success"] = "Закупівлю додано, склад поповнено";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id is null) return NotFound();

            var purchase = _uow.InventoryPurchase.Get(
                p => p.Id == id.Value,
                includeProperties: "InventoryItem");
            if (purchase is null) return NotFound();

            var model = new InventoryPurchaseViewModel
            {
                Id = purchase.Id,
                ItemName = purchase.InventoryItem?.ItemName ?? "—",
                Unit = purchase.InventoryItem?.Unit ?? "",
                Quantity = purchase.Quantity,
                PricePerUnit = purchase.PricePerUnit,
                TotalCost = purchase.TotalCost,
                Supplier = purchase.Supplier,
                PurchaseDate = purchase.PurchaseDate,
            };
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            if (id is null) return NotFound();
            var purchase = _uow.InventoryPurchase.Get(
                p => p.Id == id.Value,
                includeProperties: "InventoryItem",
                tracked: true);

            if (purchase is null) return NotFound();

            _service.RemovePurchase(purchase);

            TempData["Success"] = "Запис закупівлі видалено, залишок на складі скориговано";
            return RedirectToAction(nameof(Index));
        }

        private void PopulateDropdowns(CreateInventoryPurchaseViewModel model)
        {
            model.InventoryItems = _uow.InventoryItem
                .GetAll()
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = $"{i.ItemName} (залишок: {i.Quantity} {i.Unit})"
                })
                .ToList();
        }
    }
}
