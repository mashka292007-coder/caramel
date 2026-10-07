using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.Services.ExternalApi.Interfaces;
using CaramelCloud.UI.Models.InstagramThreads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CaramelCloud.UI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class InstagramThreadsController : Controller
    {
        private readonly IInstagramService _instagram;
        private readonly IThreadsService _threads;
        private readonly IFacebookService _facebook;
        private readonly IUnitOfWork _unitOfWork;

        public InstagramThreadsController(
            IInstagramService instagram,
            IThreadsService threads,
            IFacebookService facebook,
            IUnitOfWork unitOfWork)
        {
            _instagram = instagram;
            _threads = threads;
            _facebook = facebook;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new InstagramThreadsPostViewModel
            {
                Products = _unitOfWork.Product.GetAll()
                    .Where(p => !string.IsNullOrEmpty(p.ImageUrl))
                    .Select(p => new SelectListItem
                    {
                        Value = p.ImageUrl,
                        Text = p.Name
                    })
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(
            InstagramThreadsPostViewModel model,
            bool publishToInstagram = false,
            bool publishToThreads = false,
            bool publishToFacebook = false)
        {
            if (!ModelState.IsValid)
            {
                model.Products = _unitOfWork.Product.GetAll()
                    .Where(p => !string.IsNullOrEmpty(p.ImageUrl))
                    .Select(p => new SelectListItem
                    {
                        Value = p.ImageUrl,
                        Text = p.Name
                    })
                    .ToList();
                return View("Create", model);
            }

            var messages = new List<string>();

            if (publishToInstagram)
            {
                var igSuccess = await _instagram.PublishPostAsync(model.ImageUrl, model.Caption);
                messages.Add(igSuccess
                    ? "Instagram: успішно"
                    : "Instagram: помилка");
            }

            if (publishToThreads)
            {
                var threadsResult = await _threads.PublishPostAsync(model.Caption, model.ImageUrl);
                messages.Add(threadsResult.Success
                    ? "Threads: успішно"
                    : $"Threads: помилка ({threadsResult.ErrorMessage})");
            }

            if (publishToFacebook)
            {
                var fbResult = await _facebook.PublishPostAsync(model.Caption, model.ImageUrl);
                messages.Add(fbResult.Success
                    ? "Facebook: успішно"
                    : $"Facebook: помилка ({fbResult.ErrorMessage})");
            }

            var allSuccess = messages.All(m => m.Contains("успішно"));
            TempData[allSuccess ? "Success" : "ErrorMessage"] =
                "Результат публікації: " + string.Join(" | ", messages);

            return RedirectToAction(nameof(Create));
        }
    }
}