using Microsoft.AspNetCore.Identity;
using ELearningWebsite.Models;

namespace ELearningWebsite.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Create roles
            string[] roleNames = { "Admin", "Instructor", "Student" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                }
            }

            // Create admin user
            var adminUser = await userManager.FindByEmailAsync("admin@elearning.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@elearning.com",
                    Email = "admin@elearning.com",
                    FullName = "Administrator",
                    EmailConfirmed = true,
                    IsVerified = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Create instructor user
            var instructorUser = await userManager.FindByEmailAsync("instructor@elearning.com");
            if (instructorUser == null)
            {
                instructorUser = new ApplicationUser
                {
                    UserName = "instructor@elearning.com",
                    Email = "instructor@elearning.com",
                    FullName = "Instructor Demo",
                    EmailConfirmed = true,
                    IsVerified = true
                };

                var result = await userManager.CreateAsync(instructorUser, "Instructor@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(instructorUser, "Instructor");
                }
            }

            // Create student user
            var studentUser = await userManager.FindByEmailAsync("student@elearning.com");
            if (studentUser == null)
            {
                studentUser = new ApplicationUser
                {
                    UserName = "student@elearning.com",
                    Email = "student@elearning.com",
                    FullName = "Student Demo",
                    EmailConfirmed = true,
                    IsVerified = true
                };

                var result = await userManager.CreateAsync(studentUser, "Student@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(studentUser, "Student");
                }
            }
        }
    }
}
