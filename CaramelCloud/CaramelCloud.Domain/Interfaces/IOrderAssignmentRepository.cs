using CaramelCloud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace CaramelCloud.Domain.Interfaces
{
    public interface IOrderAssignmentRepository : IRepository<OrderAssignment>
    {
        void Update(OrderAssignment entity);
        void UpdateStatus(int assignmentId, int statusId);
        Employee? GetLeastBusyEmployee(int positionId);
        IEnumerable<OrderAssignment> GetAllTracked(Expression<Func<OrderAssignment, bool>> filter);
    }
}
