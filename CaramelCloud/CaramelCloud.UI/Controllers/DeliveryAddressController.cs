using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using CaramelCloud.UI.Models.DeliveryAddress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CaramelCloud.UI.Controllers
{
    [Authorize]
    public class DeliveryAddressController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IUnitOfWork _uow;

        public DeliveryAddressController(UserManager<User> userManager, IUnitOfWork uow)
        {
            _userManager = userManager;
            _uow = uow;
        }

        [HttpGet]
        public IActionResult Create(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new CreateDeliveryAddressViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateDeliveryAddressViewModel model, string? returnUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var userId = int.Parse(_userManager.GetUserId(User)!);

            var address = new DeliveryAddress
            {
                UserId = userId,
                City = model.City.Trim(),
                AddressLine = model.AddressLine.Trim(),
                PostalCode = model.PostalCode?.Trim(),
                IsDefault = model.IsDefault
            };

            _uow.DeliveryAddress.Add(address);
            _uow.Save();

            TempData["Success"] = "Адреса успішно додана!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("PlaceOrder", "Order");
        }

        [HttpGet]
        public IActionResult Index()
        {
            var userId = int.Parse(_userManager.GetUserId(User)!);
            var addresses = _uow.DeliveryAddress
                .GetAll(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ToList();

            return View(addresses);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var userId = int.Parse(_userManager.GetUserId(User)!);
            var address = _uow.DeliveryAddress.Get(a => a.Id == id && a.UserId == userId);

            if (address is not null)
            {
                _uow.DeliveryAddress.Remove(address);
                _uow.Save();
                TempData["Success"] = "Адреса видалена";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
