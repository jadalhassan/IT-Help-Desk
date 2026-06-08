using HelpDesk.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Data;

public static class DbSeeder
{
    private static readonly string[] Categories = ["Bug", "Feature Request", "Support", "Billing", "General"];

    public static async Task SeedAsync(AppDbContext db)
    {
        await EnsureTicketTableAsync(db);

        if (await db.Users.AnyAsync())
        {
            await SeedTicketsAsync(db);
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

        await SeedTicketsAsync(db);
    }

    private static async Task EnsureTicketTableAsync(AppDbContext db)
    {
        if (db.Database.IsSqlite())
        {
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "Tickets" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_Tickets" PRIMARY KEY AUTOINCREMENT,
                    "Title" TEXT NOT NULL,
                    "Description" TEXT NOT NULL,
                    "Category" TEXT NOT NULL,
                    "Priority" TEXT NOT NULL,
                    "Status" TEXT NOT NULL,
                    "CreatedAtUtc" TEXT NOT NULL,
                    "UpdatedAtUtc" TEXT NOT NULL
                );
                """);
        }
    }

    private static async Task SeedTicketsAsync(AppDbContext db)
    {
        if (await db.Tickets.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var tickets = new List<Ticket>
        {
            new()
            {
                Title = "Cannot access email",
                Description = "User receives an invalid credentials message after password reset.",
                Category = Categories[2],
                Priority = "High",
                Status = "Open",
                CreatedAtUtc = now.AddHours(-7),
                UpdatedAtUtc = now.AddHours(-7)
            },
            new()
            {
                Title = "Invoice export missing columns",
                Description = "Billing export should include department and purchase order fields.",
                Category = Categories[3],
                Priority = "Medium",
                Status = "In Progress",
                CreatedAtUtc = now.AddDays(-1),
                UpdatedAtUtc = now.AddHours(-3)
            },
            new()
            {
                Title = "Add dark mode preference",
                Description = "Users requested a persistent appearance setting in the portal.",
                Category = Categories[1],
                Priority = "Low",
                Status = "Resolved",
                CreatedAtUtc = now.AddDays(-3),
                UpdatedAtUtc = now.AddDays(-1)
            }
        };

        db.Tickets.AddRange(tickets);
        await db.SaveChangesAsync();
    }
}
