using CaramelCloud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.Interfaces
{
    public interface IOrderAssignmentService
    {
        void AssignEmployee(OrderAssignment assignment);
        void UpdateAssignmentStatus(int assignmentId, int statusId);
    }
}
