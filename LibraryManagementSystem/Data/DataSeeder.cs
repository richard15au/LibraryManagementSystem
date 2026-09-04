using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagementSystem.Data
{
    public static class DataSeeder
    {
        public static async Task SeedRolesAsync(
            RoleManager<IdentityRole> roleManager)
        {
            string[] roles =
            {
                "Member",
                "Librarian"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }
        }

        public static async Task SeedLibrarianAsync(
            UserManager<ApplicationUser> userManager)
        {
            const string librarianEmail = "librarian@library.local";
            const string librarianPassword = "Library123!";

            var existingUser =
                await userManager.FindByEmailAsync(librarianEmail);

            if (existingUser == null)
            {
                var librarian = new ApplicationUser
                {
                    UserName = librarianEmail,
                    Email = librarianEmail,

                    FirstName = "System",
                    LastName = "Librarian",

                    EmailConfirmed = true,
                    IsActive = true,
                    DateRegistered = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(
                    librarian,
                    librarianPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(
                        librarian,
                        "Librarian");
                }
                else
                {
                    var errors = string.Join(
                        "; ",
                        result.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Could not create librarian: {errors}");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(
                        existingUser,
                        "Librarian"))
                {
                    await userManager.AddToRoleAsync(
                        existingUser,
                        "Librarian");
                }
            }
        }
    }
}