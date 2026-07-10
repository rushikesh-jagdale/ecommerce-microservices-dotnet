using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using UserService.Domain.Entities;
using UserService.Domain.Enums;

namespace UserService.Infrastructure.Persistence
{
    public static class UserDbSeeder
    {
        public static async Task SeedAdminAsync(
            UserDbContext context,
            IConfiguration configuration)
        {
            var adminEmail = configuration["AdminUser:Email"];

            if (await context.Users.AnyAsync(u => u.Email == adminEmail))
                return;

            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = adminEmail!,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    configuration["AdminUser:Password"]!),

                FirstName = configuration["AdminUser:FirstName"]!,
                LastName = configuration["AdminUser:LastName"]!,

                Role = UserRole.Admin
            };

            context.Users.Add(admin);

            await context.SaveChangesAsync();
        }
    }
}