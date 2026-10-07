using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class OrderAssignmentRepository : Repository<OrderAssignment>, IOrderAssignmentRepository
    {
        public OrderAssignmentRepository(AppDbContext db) : base(db) { }

        public void Update(OrderAssignment entity)
        {
            var obj = _db.OrderAssignments.FirstOrDefault(x => x.Id == entity.Id);
            if (obj is not null)
            {
                obj.AssignmentStatusId = entity.AssignmentStatusId;
                obj.Notes = entity.Notes;
                obj.CompletedAt = entity.CompletedAt;
            }
        }

        public void UpdateStatus(int assignmentId, int statusId)
        {
            var obj = _db.OrderAssignments.FirstOrDefault(x => x.Id == assignmentId);
            if (obj is not null)
            {
                obj.AssignmentStatusId = statusId;
                if (statusId == 3)
                    obj.CompletedAt = DateTime.UtcNow;
            }
        }
        public Employee? GetLeastBusyEmployee(int positionId)
        {
            const int maxActiveAssignments = 4;

            return _db.Employees
                .Where(e => e.PositionId == positionId && e.IsActive)
                .Select(e => new
                {
                    Employee = e,
                    ActiveCount = e.OrderAssignments.Count(a => a.CompletedAt == null)
                })
                .Where(x => x.ActiveCount < maxActiveAssignments) 
                .OrderBy(x => x.ActiveCount)
                .Select(x => x.Employee)
                .FirstOrDefault();
        }
        public IEnumerable<OrderAssignment> GetAllTracked(Expression<Func<OrderAssignment, bool>> filter)
        {
            return _db.OrderAssignments
                .Where(filter)
                .ToList();
        }
    }
}
