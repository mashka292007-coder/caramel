namespace CaramelCloud.UI.Models.OrderAssignment
{
    public class AssignmentViewModel
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string AssignmentType { get; set; } = string.Empty;
        public string AssignmentStatus { get; set; } = string.Empty;
        public int AssignmentStatusId { get; set; }
        public string AssignedBy { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
