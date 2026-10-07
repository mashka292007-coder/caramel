using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using CaramelCloud.Infrastructure.Services.Interfaces;
using CaramelCloud.UI.Models.OrderAssignment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CaramelCloud.UI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderAssignmentController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IOrderAssignmentService _service;
        private readonly UserManager<User> _userManager;

        public OrderAssignmentController(
            IUnitOfWork uow,
            IOrderAssignmentService service,
            UserManager<User> userManager)
        {
            _uow = uow;
            _service = service;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Create(int orderId)
        {
            var model = new CreateAssignmentViewModel { OrderId = orderId };
            PopulateDropdowns(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateAssignmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns(model);
                return View(model);
            }

            var assignment = new OrderAssignment
            {
                OrderId = model.OrderId,
                EmployeeId = model.EmployeeId,
                AssignmentTypeId = model.AssignmentTypeId,
                AssignmentStatusId = 1, 
                AssignedByUserId = int.Parse(_userManager.GetUserId(User)!),
                Notes = model.Notes?.Trim(),
                AssignedAt = DateTime.UtcNow,
            };

            _service.AssignEmployee(assignment);

            TempData["Success"] = "Завдання успішно призначено";
            return RedirectToAction("Details", "Order", new { id = model.OrderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int assignmentId, int statusId, int orderId)
        {
            _service.UpdateAssignmentStatus(assignmentId, statusId);
            TempData["Success"] = "Статус завдання оновлено";
            return RedirectToAction("Details", "Order", new { id = orderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, int orderId)
        {
            var assignment = _uow.OrderAssignment.Get(a => a.Id == id);
            if (assignment is not null)
            {
                _uow.OrderAssignment.Remove(assignment);
                _uow.Save();
                TempData["Success"] = "Призначення видалено";
            }
            return RedirectToAction("Details", "Order", new { id = orderId });
        }

        private void PopulateDropdowns(CreateAssignmentViewModel model)
        {
            model.Employees = _uow.Employee
                .GetAll(e => e.IsActive, includeProperties: "Position")
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = $"{e.FirstName} {e.LastName} ({e.Position?.Name})"
                }).ToList();

            model.AssignmentTypes = _uow.AssignmentType
                .GetAll()
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToList();
        }
    }
}
