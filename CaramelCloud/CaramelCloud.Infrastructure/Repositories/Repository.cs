using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _db;
        protected readonly DbSet<T> DbSet;

        public Repository(AppDbContext db)
        {
            _db = db;
            DbSet = _db.Set<T>();
        }

        public IEnumerable<T> GetAll(
            Expression<Func<T, bool>>? filter = null,
            string? includeProperties = null)
        {
            IQueryable<T> query = DbSet;

            if (filter != null)
                query = query.Where(filter);

            if (!string.IsNullOrWhiteSpace(includeProperties))
            {
                foreach (var prop in includeProperties
                    .Split(',', StringSplitOptions.RemoveEmptyEntries))
                    query = query.Include(prop.Trim());
            }

            return query.ToList();
        }

        public T? Get(
            Expression<Func<T, bool>> filter,
            string? includeProperties = null,
            bool tracked = false)
        {
            IQueryable<T> query = tracked ? DbSet : DbSet.AsNoTracking();

            query = query.Where(filter);

            if (!string.IsNullOrWhiteSpace(includeProperties))
            {
                foreach (var prop in includeProperties
                    .Split(',', StringSplitOptions.RemoveEmptyEntries))
                    query = query.Include(prop.Trim());
            }

            return query.FirstOrDefault();
        }

        public void Add(T entity) => DbSet.Add(entity);
        public void Remove(T entity) => DbSet.Remove(entity);
        public void RemoveRange(IEnumerable<T> entities) => DbSet.RemoveRange(entities);
    }
}
