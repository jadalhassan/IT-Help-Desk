using HelpDesk.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        var users = new List<User>
        {
            new()
            {
                FullName = "System Admin",
                Email = "admin@helpdesk.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "Admin"
            },
            new()
            {
                FullName = "Support Agent",
                Email = "agent@helpdesk.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Agent@123"),
                Role = "Agent"
            },
            new()
            {
                FullName = "Standard User",
                Email = "user@helpdesk.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                Role = "User"
            }
        };

        db.Users.AddRange(users);
        await db.SaveChangesAsync();
    }
}