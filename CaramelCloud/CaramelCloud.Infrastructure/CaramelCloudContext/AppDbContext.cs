using CaramelCloud.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.CaramelCloudContext
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Свої DbSet — всі залишаються
        public DbSet<Role> Roles => Set<Role>();              // ← твій довідник ролей
        public DbSet<Position> Positions => Set<Position>();
        public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
        public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
        public DbSet<DeliveryMethod> DeliveryMethods => Set<DeliveryMethod>();
        public DbSet<AssignmentType> AssignmentTypes => Set<AssignmentType>();
        public DbSet<AssignmentStatus> AssignmentStatuses => Set<AssignmentStatus>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
        public DbSet<DeliveryAddress> DeliveryAddresses => Set<DeliveryAddress>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<OrderAssignment> OrderAssignments => Set<OrderAssignment>();
        public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
        public DbSet<InventoryPurchase> InventoryPurchases => Set<InventoryPurchase>();
        public DbSet<Recipe> Recipes => Set<Recipe>();
        public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<CakeLayerType> CakeLayerTypes => Set<CakeLayerType>();
        public DbSet<CakeFillingType> CakeFillingTypes => Set<CakeFillingType>();
        public DbSet<CustomCake> CustomCakes => Set<CustomCake>();
        public DbSet<CustomCakeLayer> CustomCakeLayers => Set<CustomCakeLayer>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // ← ОБОВ'ЯЗКОВО — будує таблиці Identity

            // Перейменовуємо таблиці Identity щоб не конфліктували з твоєю "Roles"
            modelBuilder.Entity<User>().ToTable("AspNetUsers");
            modelBuilder.Entity<IdentityRole<int>>().ToTable("AspNetRoles");
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("AspNetUserRoles");
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("AspNetUserClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("AspNetUserLogins");
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("AspNetUserTokens");
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("AspNetRoleClaims");

            // Твоя таблиця ролей-довідник — окрема назва
            modelBuilder.Entity<Role>().ToTable("Roles");

            // Зв'язок User → своя Role (довідник)
            modelBuilder.Entity<User>(e =>
            {
                e.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(u => u.CustomerProfile)
                    .WithOne(cp => cp.User)
                    .HasForeignKey<CustomerProfile>(cp => cp.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(u => u.DeliveryAddresses)
                    .WithOne(da => da.User)
                    .HasForeignKey(da => da.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(u => u.CreatedOrders)
                    .WithOne(o => o.CreatedByUser)
                    .HasForeignKey(o => o.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasMany(u => u.AssignedByAssignments)
                    .WithOne(a => a.AssignedByUser)
                    .HasForeignKey(a => a.AssignedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasMany(u => u.CustomCakes)
                    .WithOne(cc => cc.User)
                    .HasForeignKey(cc => cc.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Решта конфігурацій — всі залишаються без змін
            modelBuilder.Entity<CustomerProfile>(e =>
            {
                e.HasMany(cp => cp.Orders)
                    .WithOne(o => o.CustomerProfile)
                    .HasForeignKey(o => o.CustomerProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DeliveryAddress>(e =>
            {
                e.HasMany(da => da.Orders)
                    .WithOne(o => o.DeliveryAddress)
                    .HasForeignKey(o => o.DeliveryAddressId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Employee>(e =>
            {
                e.HasOne(emp => emp.Position)
                    .WithMany(p => p.Employees)
                    .HasForeignKey(emp => emp.PositionId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasMany(emp => emp.OrderAssignments)
                    .WithOne(a => a.Employee)
                    .HasForeignKey(a => a.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OrderAssignment>(e =>
            {
                e.HasOne(a => a.Order)
                    .WithMany(o => o.OrderAssignments)
                    .HasForeignKey(a => a.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(a => a.AssignmentType)
                    .WithMany(at => at.OrderAssignments)
                    .HasForeignKey(a => a.AssignmentTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(a => a.AssignmentStatus)
                    .WithMany(s => s.OrderAssignments)
                    .HasForeignKey(a => a.AssignmentStatusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InventoryPurchase>(e =>
            {
                e.HasOne(ip => ip.InventoryItem)
                    .WithMany(ii => ii.Purchases)
                    .HasForeignKey(ip => ip.InventoryItemId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Recipe>(e =>
            {
                e.HasMany(r => r.RecipeIngredients)
                    .WithOne(ri => ri.Recipe)
                    .HasForeignKey(ri => ri.RecipeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RecipeIngredient>(e =>
            {
                e.HasOne(ri => ri.InventoryItem)
                    .WithMany(ii => ii.RecipeIngredients)
                    .HasForeignKey(ri => ri.InventoryItemId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Product>(e =>
            {
                e.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.Recipe)
                    .WithOne(r => r.Product!)
                    .HasForeignKey<Product>(p => p.RecipeId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasMany(p => p.OrderItems)
                    .WithOne(oi => oi.Product)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.Property(p => p.Price).HasPrecision(18, 2);
                e.Property(p => p.Weight).HasPrecision(18, 3);
            });

            modelBuilder.Entity<CakeLayerType>(e =>
            {
                e.HasOne(lt => lt.Recipe)
                    .WithOne(r => r.CakeLayerType!)
                    .HasForeignKey<CakeLayerType>(lt => lt.RecipeId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasMany(lt => lt.CustomCakeLayers)
                    .WithOne(cl => cl.LayerType)
                    .HasForeignKey(cl => cl.LayerTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.Property(lt => lt.ExtraPrice).HasPrecision(18, 2);
            });

            modelBuilder.Entity<CakeFillingType>(e =>
            {
                e.HasOne(ft => ft.Recipe)
                    .WithOne(r => r.CakeFillingType!)
                    .HasForeignKey<CakeFillingType>(ft => ft.RecipeId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasMany(ft => ft.CustomCakeLayers)
                    .WithOne(cl => cl.FillingType)
                    .HasForeignKey(cl => cl.FillingTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.Property(ft => ft.ExtraPrice).HasPrecision(18, 2);
            });

            modelBuilder.Entity<CustomCake>(e =>
            {
                e.HasMany(cc => cc.CustomCakeLayers)
                    .WithOne(cl => cl.CustomCake)
                    .HasForeignKey(cl => cl.CustomCakeId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasMany(cc => cc.OrderItems)
                    .WithOne(oi => oi.CustomCake)
                    .HasForeignKey(oi => oi.CustomCakeId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.Property(cc => cc.TotalPrice).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Order>(e =>
            {
                e.HasOne(o => o.Status)
                    .WithMany(s => s.Orders)
                    .HasForeignKey(o => o.StatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(o => o.PaymentMethod)
                    .WithMany(pm => pm.Orders)
                    .HasForeignKey(o => o.PaymentMethodId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(o => o.DeliveryMethod)
                    .WithMany(dm => dm.Orders)
                    .HasForeignKey(o => o.DeliveryMethodId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasMany(o => o.OrderItems)
                    .WithOne(oi => oi.Order)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.Property(o => o.TotalAmount).HasPrecision(18, 2);
            });

            modelBuilder.Entity<OrderItem>(e =>
            {
                e.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            });

            modelBuilder.Entity<InventoryItem>(e =>
            {
                e.Property(ii => ii.Quantity).HasPrecision(18, 3);
                e.Property(ii => ii.MinQuantity).HasPrecision(18, 3);
            });

            modelBuilder.Entity<InventoryPurchase>(e =>
            {
                e.Property(ip => ip.Quantity).HasPrecision(18, 3);
                e.Property(ip => ip.PricePerUnit).HasPrecision(18, 2);
                e.Property(ip => ip.TotalCost).HasPrecision(18, 2);
            });

            modelBuilder.Entity<RecipeIngredient>(e =>
            {
                e.Property(ri => ri.Quantity).HasPrecision(18, 3);
            });
        }

    }
}
