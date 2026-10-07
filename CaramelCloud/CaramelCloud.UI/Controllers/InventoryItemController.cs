using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.UI.Models.InventoryItem;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaramelCloud.UI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InventoryItemController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public InventoryItemController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var items = _unitOfWork.InventoryItem
                .GetAll()
                .Select(i => new InventoryItemViewModel
                {
                    Id = i.Id,
                    ItemName = i.ItemName,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    MinQuantity = i.MinQuantity,
                    LastUpdated = i.LastUpdated
                })
                .OrderBy(i => i.ItemName)
                .ToList();

            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateInventoryItemViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateInventoryItemViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var item = new InventoryItem
            {
                ItemName = model.ItemName.Trim(),
                Quantity = model.Quantity,
                Unit = model.Unit.Trim(),
                MinQuantity = model.MinQuantity,
                LastUpdated = DateTime.UtcNow
            };

            _unitOfWork.InventoryItem.Add(item);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Позицію складу успішно створено";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var item = _unitOfWork.InventoryItem.Get(x => x.Id == id.Value);
            if (item is null) return NotFound();

            var model = new InventoryItemViewModel
            {
                Id = item.Id,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                Unit = item.Unit,
                MinQuantity = item.MinQuantity,
                LastUpdated = item.LastUpdated
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InventoryItemViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var itemFromDb = _unitOfWork.InventoryItem.Get(x => x.Id == model.Id);
            if (itemFromDb is null) return NotFound();

            itemFromDb.ItemName = model.ItemName.Trim();
            itemFromDb.Quantity = model.Quantity;
            itemFromDb.Unit = model.Unit.Trim();
            itemFromDb.MinQuantity = model.MinQuantity;

            _unitOfWork.InventoryItem.Update(itemFromDb);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Позицію складу успішно оновлено";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var item = _unitOfWork.InventoryItem.Get(x => x.Id == id.Value);
            if (item is null) return NotFound();

            var model = new InventoryItemViewModel
            {
                Id = item.Id,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                Unit = item.Unit,
                MinQuantity = item.MinQuantity,
                LastUpdated = item.LastUpdated
            };

            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int? id)
        {
            if (id is null || id == 0) return NotFound();

            var item = _unitOfWork.InventoryItem.Get(x => x.Id == id.Value);
            if (item is null) return NotFound();

            _unitOfWork.InventoryItem.Remove(item);
            await _unitOfWork.SaveAsync();

            TempData["success"] = "Позицію складу успішно видалено";
            return RedirectToAction(nameof(Index));
        }
    }
}
