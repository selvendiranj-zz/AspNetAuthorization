using ContactManager.Authorization;
using ContactManager.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

// dotnet aspnet-codegenerator razorpage -m Contact -dc ApplicationDbContext -outDir Pages\Contacts --referenceScriptLibraries
namespace ContactManager.Data
{
    public static class SeedData
    {
        private static UserManager<ApplicationUser> _userManager;
        private static RoleManager<IdentityRole> _roleManager;

        public static async Task Initialize(IServiceProvider serviceProvider, string testUserPw)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // For sample purposes we are seeding 2 users both with the same password.
                // The password is set with the following command:
                // dotnet user-secrets set SeedUserPW <pw>
                // The admin user can do anything

                var adminId = await EnsureUser(serviceProvider, "admin@contoso.com", testUserPw);
                await EnsureRole(serviceProvider, adminId, Constants.ContactAdministratorsRole);

                // allowed user can create and edit contacts that they create
                var mgrId = await EnsureUser(serviceProvider, "manager@contoso.com", testUserPw);
                await EnsureRole(serviceProvider, mgrId, Constants.ContactManagersRole);

                SeedDb(context, adminId);
            }
        }

        private static async Task<string> EnsureUser(IServiceProvider serviceProvider,
            string userName, string testUserPw)
        {
            _userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();

            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                user = new ApplicationUser { UserName = userName };
                var result = await _userManager.CreateAsync(user, testUserPw);
            }

            return user.Id;
        }

        private static async Task<IdentityResult> EnsureRole(IServiceProvider serviceProvider,
            string userId, string role)
        {
            IdentityResult ir = null;
            _roleManager = serviceProvider.GetService<RoleManager<IdentityRole>>();

            if (!await _roleManager.RoleExistsAsync(role))
            {
                ir = await _roleManager.CreateAsync(new IdentityRole(role));
            }

            _userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();

            var user = await _userManager.FindByIdAsync(userId);

            ir = await _userManager.AddToRoleAsync(user, role);

            return ir;
        }

        public static void SeedDb(ApplicationDbContext context, string adminId)
        {
            if (context.Contact.Any())
            {
                return;   // DB has been seeded
            }

            context.Contact.AddRange(
                new Contact
                {
                    Name = "Debra Garcia",
                    Address = "1234 Main St",
                    City = "Redmond",
                    State = "WA",
                    Zip = "10999",
                    Email = "debra@example.com",
                    Status = ContactStatus.Approved,
                    OwnerID = adminId
                },

                new Contact
                {
                    Name = "Thorsten Weinrich",
                    Address = "5678 1st Ave W",
                    City = "Redmond",
                    State = "WA",
                    Zip = "10999",
                    Email = "thorsten@example.com",
                    Status = ContactStatus.Approved,
                    OwnerID = adminId
                },
                new Contact
                {
                    Name = "Yuhong Li",
                    Address = "9012 State st",
                    City = "Redmond",
                    State = "WA",
                    Zip = "10999",
                    Email = "yuhong@example.com",
                    Status = ContactStatus.Approved,
                    OwnerID = adminId
                },
                new Contact
                {
                    Name = "Jon Orton",
                    Address = "3456 Maple St",
                    City = "Redmond",
                    State = "WA",
                    Zip = "10999",
                    Email = "jon@example.com",
                    OwnerID = adminId
                },
                new Contact
                {
                    Name = "Diliana Alexieva-Bosseva",
                    Address = "7890 2nd Ave E",
                    City = "Redmond",
                    State = "WA",
                    Zip = "10999",
                    Email = "diliana@example.com",
                    OwnerID = adminId
                }
             );
            context.SaveChanges();
        }
    }
}