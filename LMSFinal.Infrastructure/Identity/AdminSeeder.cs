using LMSFinal.Domain.Entities;
using LMSFinal.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Infrastructure.Identity
{
    public class AdminSeeder
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AdminSeeder(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task SeedAsync()
        {
            var email = _configuration["Seed:AdminEmail"];
            var password = _configuration["Seed:AdminPassword"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                return;
            }

            var admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "Admin",
                LastName = "LMSFinal",
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(admin, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(admin, nameof(UserRole.Admin));
            }
        }
    }

}
