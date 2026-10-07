using CaramelCloud.Domain.Entities;
using CaramelCloud.Domain.Interfaces;
using CaramelCloud.Infrastructure.CaramelCloudContext;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _db;

        public ICategoryRepository Category { get; private set; }
        public IProductRepository Product { get; private set; }
        public IEmployeeRepository Employee { get; private set; }
        public IInventoryItemRepository InventoryItem { get; private set; }
        public IRecipeRepository Recipe { get; private set; }
        public IRecipeIngredientRepository RecipeIngredient { get; private set; }
        public IPositionRepository Position { get; private set; }
        public IOrderRepository Order { get; private set; }
        public IRepository<OrderItem> OrderItem { get; private set; }
        public IOrderAssignmentRepository OrderAssignment { get; private set; }
        public IInventoryPurchaseRepository InventoryPurchase { get; private set; }
        public IRepository<DeliveryAddress> DeliveryAddress { get; private set; }

        public ICustomCakeRepository CustomCake { get; private set; }
        public IRepository<CustomCakeLayer> CustomCakeLayer { get; private set; }
        public IRepository<CakeLayerType> CakeLayerType { get; private set; }
        public IRepository<CakeFillingType> CakeFillingType { get; private set; }

        public ICustomerProfileRepository CustomerProfile { get; private set; }
        public IRoleRepository Role { get; private set; }
        public IRepository<PaymentMethod> PaymentMethod { get; private set; }
        public IRepository<DeliveryMethod> DeliveryMethod { get; private set; }

        public IRepository<AssignmentType> AssignmentType { get; private set; }
        public IRepository<OrderStatus> OrderStatus { get; private set ; }

        public UnitOfWork(AppDbContext db)
        {
            _db = db;
            Category = new CategoryRepository(_db);
            Product = new ProductRepository(_db);
            Employee = new EmployeeRepository(_db);
            InventoryItem = new InventoryItemRepository(_db);
            Recipe = new RecipeRepository(_db);
            RecipeIngredient = new RecipeIngredientRepository(_db);
            Position = new PositionRepository(_db);
            Order = new OrderRepository(_db);
            OrderItem = new Repository<OrderItem>(_db);
            OrderAssignment = new OrderAssignmentRepository(_db);
            InventoryPurchase = new InventoryPurchaseRepository(_db);
            CustomerProfile = new CustomerProfileRepository(_db);
            DeliveryAddress = new Repository<DeliveryAddress>(db);
            CustomCake = new CustomCakeRepository(_db);
            CustomCakeLayer = new Repository<CustomCakeLayer>(_db);
            CakeLayerType = new Repository<CakeLayerType>(_db);
            CakeFillingType = new Repository<CakeFillingType>(_db);
            Role = new RoleRepository(_db);
            PaymentMethod = new Repository<PaymentMethod>(_db);
            DeliveryMethod = new Repository<DeliveryMethod>(_db);
            AssignmentType = new Repository<AssignmentType>(_db);
            OrderStatus = new Repository<OrderStatus>(_db);
        }
        public void Save() => _db.SaveChanges();
        public async Task SaveAsync() => await _db.SaveChangesAsync();
    }
}
