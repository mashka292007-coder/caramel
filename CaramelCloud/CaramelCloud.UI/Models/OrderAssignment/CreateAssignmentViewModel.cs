using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CaramelCloud.UI.Models.OrderAssignment
{
    public class CreateAssignmentViewModel
    {
        public int OrderId { get; set; }

        [DisplayName("Працівник")]
        [Required(ErrorMessage = "Оберіть працівника")]
        public int EmployeeId { get; set; }

        [DisplayName("Тип завдання")]
        [Required(ErrorMessage = "Оберіть тип завдання")]
        public int AssignmentTypeId { get; set; }

        [DisplayName("Нотатки")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        public IEnumerable<SelectListItem> Employees { get; set; } = [];
        public IEnumerable<SelectListItem> AssignmentTypes { get; set; } = [];
    }
}
