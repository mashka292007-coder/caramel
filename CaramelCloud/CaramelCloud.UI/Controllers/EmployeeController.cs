using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.UI.Models.Employee;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CaramelCloud.UI.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly IUnitOfWork _uow;

        public EmployeeController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public IActionResult Index()
        {
            var employees = _uow.Employee.GetAll(includeProperties: "Position");

            var viewModels = employees.Select(e => new EmployeeViewModel
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Phone = e.Phone,
                PositionId = e.PositionId,
                HiredAt = e.HiredAt,
                IsActive = e.IsActive,
                PositionName = e.Position?.Name ?? "—"
            }).ToList();

            return View(viewModels);
        }

        public IActionResult Create()
        {
            var vm = new CreateEmployeeViewModel
            {
                Positions = GetPositionSelectList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEmployeeViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Positions = GetPositionSelectList();
                return View(vm);
            }

            var employee = new Employee
            {
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Phone = vm.Phone,
                PositionId = vm.PositionId,
                HiredAt = vm.HiredAt,
                IsActive = vm.IsActive
            };

            _uow.Employee.Add(employee);
            await _uow.SaveAsync();

            TempData["Success"] = "Працівника успішно додано.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var employee = _uow.Employee.Get(e => e.Id == id, includeProperties: "Position");
            if (employee is null)
                return NotFound();

            var vm = new EmployeeViewModel
            {
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Phone = employee.Phone,
                PositionId = employee.PositionId,
                HiredAt = employee.HiredAt,
                IsActive = employee.IsActive,
                PositionName = employee.Position?.Name ?? "—",
                Positions = GetPositionSelectList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmployeeViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Positions = GetPositionSelectList();
                return View(vm);
            }

            var employee = new Employee
            {
                Id = vm.Id,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Phone = vm.Phone,
                PositionId = vm.PositionId,
                HiredAt = vm.HiredAt,
                IsActive = vm.IsActive
            };

            _uow.Employee.Update(employee);
            await _uow.SaveAsync();

            TempData["Success"] = "Дані працівника оновлено.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            var employee = _uow.Employee.Get(e => e.Id == id, includeProperties: "Position");
            if (employee is null)
                return NotFound();

            var vm = new EmployeeViewModel
            {
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Phone = employee.Phone,
                PositionId = employee.PositionId,
                HiredAt = employee.HiredAt,
                IsActive = employee.IsActive,
                PositionName = employee.Position?.Name ?? "—"
            };

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = _uow.Employee.Get(e => e.Id == id);
            if (employee is null)
                return NotFound();

            _uow.Employee.Remove(employee);
            await _uow.SaveAsync();

            TempData["Success"] = "Працівника видалено.";
            return RedirectToAction(nameof(Index));
        }

        private IEnumerable<SelectListItem> GetPositionSelectList()
        {
            return _uow.Position.GetAll()
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name
                })
                .ToList();
        }
    }
}
