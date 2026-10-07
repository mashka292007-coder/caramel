using CaramelCloud.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace CaramelCloud.Infrastructure.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            CaramelCloudContext.AppDbContext db)
        {
            string[] identityRoles = { "Admin", "Customer", "Employee" };
            foreach (var roleName in identityRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
            }
            if (!db.Roles.Any())
            {
                db.Roles.AddRange(
                    new Role { Name = "Admin" },
                    new Role { Name = "Customer" },
                    new Role { Name = "Employee" }
                );
                await db.SaveChangesAsync();
            }
            if (await userManager.FindByEmailAsync("admin@caramelcloud.com") == null)
            {
                var adminRole = db.Roles.First(r => r.Name == "Admin");
                var admin = new User
                {
                    FirstName = "Admin",
                    LastName = "CaramelCloud",
                    Email = "admin@caramelcloud.com",
                    UserName = "admin@caramelcloud.com",
                    RoleId = adminRole.Id,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, "Admin123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
