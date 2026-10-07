using CaramelCloud.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        ICategoryRepository Category { get; }
        IProductRepository Product { get; }
        IEmployeeRepository Employee { get; }
        IInventoryItemRepository InventoryItem { get; }
        IRecipeRepository Recipe { get; }
        IRecipeIngredientRepository RecipeIngredient { get; }
        IPositionRepository Position { get; }

        IOrderRepository Order { get; }
        IRepository<OrderItem> OrderItem { get; }
        IOrderAssignmentRepository OrderAssignment { get; }
        IInventoryPurchaseRepository InventoryPurchase { get; }
        IRepository<DeliveryAddress> DeliveryAddress { get; }

        ICustomCakeRepository CustomCake { get; }
        IRepository<CustomCakeLayer> CustomCakeLayer { get; }
        IRepository<CakeLayerType> CakeLayerType { get; }
        IRepository<CakeFillingType> CakeFillingType { get; }
        
        ICustomerProfileRepository CustomerProfile { get; }
        IRoleRepository Role { get; }
        IRepository<PaymentMethod> PaymentMethod { get; }
        IRepository<DeliveryMethod> DeliveryMethod { get; }
        IRepository<AssignmentType> AssignmentType { get; }
        IRepository<OrderStatus> OrderStatus { get; }
        void Save();
        Task SaveAsync();
    }
}
