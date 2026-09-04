using AssetFlow.Models;
using Microsoft.AspNetCore.Identity;

namespace AssetFlow.Data
{
    // Roles have to exist before anyone can be put in one, and a fresh database has
    // nobody who can approve a request. Both get sorted out on startup.
    public static class DbSeeder
    {
        public const string AdminRole = "Admin";
        public const string EmployeeRole = "Employee";

        public static async Task SeedRolesAndAdminAsync(IServiceProvider services, IConfiguration configuration)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var role in new[] { AdminRole, EmployeeRole })
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var email = configuration["AdminUser:Email"];
            var password = configuration["AdminUser:Password"];

            // No credentials configured means no admin gets created - the roles are
            // still there, so an existing account can be promoted by hand.
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var admin = await userManager.FindByEmailAsync(email);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = configuration["AdminUser:FullName"] ?? "IT Administrator",
                    Department = "IT"
                };

                var created = await userManager.CreateAsync(admin, password);

                if (!created.Succeeded)
                {
                    return;
                }
            }

            if (!await userManager.IsInRoleAsync(admin, AdminRole))
            {
                await userManager.AddToRoleAsync(admin, AdminRole);
            }

            // Accounts that were created before roles existed have no role at all,
            // which would lock them out of every page. Treat them as employees.
            foreach (var user in userManager.Users.ToList())
            {
                var roles = await userManager.GetRolesAsync(user);

                if (roles.Count == 0)
                {
                    await userManager.AddToRoleAsync(user, EmployeeRole);
                }
            }
        }
    }
}
