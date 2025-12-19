using Microsoft.AspNetCore.Identity;

namespace VeronaShop.Data.Entites
{
    public static class IdentitySeed
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<int>> roleManager)
        {
            // Roles to ensure
            var roles = new[] { "Admin", "Manager", "Customer" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var rres = await roleManager.CreateAsync(new IdentityRole<int>(role));
                    if (!rres.Succeeded)
                    {
                        var errs = string.Join("; ", rres.Errors.Select(e => e.Description));
                        Console.Error.WriteLine($"Failed to create role '{role}': {errs}");
                    }
                    else
                    {
                        Console.WriteLine($"Created role: {role}");
                    }
                }
            }

            // Seed initial admin user
            var adminEmail = "admin@veronashop.local";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    DisplayName = "Site Admin",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@1234");
                if (result.Succeeded)
                {
                    var addRole = await userManager.AddToRoleAsync(adminUser, "Admin");
                    if (!addRole.Succeeded)
                    {
                        Console.Error.WriteLine($"Failed to add admin to role: {string.Join(';', addRole.Errors.Select(e => e.Description))}");
                    }
                    else
                    {
                        Console.WriteLine("Created initial admin user and added to Admin role");
                    }
                }
                else
                {
                    Console.Error.WriteLine($"Failed to create admin user: {string.Join(';', result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}