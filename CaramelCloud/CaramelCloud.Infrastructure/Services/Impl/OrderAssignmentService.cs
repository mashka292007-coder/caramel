using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Services.Impl
{
    public class OrderAssignmentService : IOrderAssignmentService
    {
        private readonly IUnitOfWork _uow;

        public OrderAssignmentService(IUnitOfWork uow) => _uow = uow;

        public void AssignEmployee(OrderAssignment assignment)
        {
            _uow.OrderAssignment.Add(assignment);
            _uow.Save();
        }

        public void UpdateAssignmentStatus(int assignmentId, int statusId)
        {
            _uow.OrderAssignment.UpdateStatus(assignmentId, statusId);
            _uow.Save();

            if (statusId == 3)
            {
                var assignment = _uow.OrderAssignment.Get(
                    a => a.Id == assignmentId,
                    tracked: true);

                if (assignment is null) return;

                int orderId = assignment.OrderId;

                var allAssignments = _uow.OrderAssignment
                    .GetAllTracked(a => a.OrderId == orderId)
                    .ToList();

                bool allDone = allAssignments.All(a => a.AssignmentStatusId == 3);

                if (allDone)
                {
                    bool hasDelivery = allAssignments.Any(a => a.AssignmentTypeId == 4);

                    int newOrderStatus = hasDelivery ? 4 : 3;

                    _uow.Order.UpdateStatus(orderId, newOrderStatus);
                    _uow.Save();
                }
            }
        }
    }
}
