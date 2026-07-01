using HelpDesk.Api.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Data;

public static class DbSeeder
{
    private static readonly string[] Categories = ["Bug", "Feature Request", "Support", "Billing", "General"];

    public static async Task SeedAsync(AppDbContext db, IConfiguration configuration, ILogger logger)
    {
        await EnsureSchemaAsync(db);

        if (configuration.GetValue<bool>("DemoMode"))
        {
            await EnsureDemoUsersAsync(db, logger);
            await SeedTicketsAsync(db);
            return;
        }

        if (await db.Users.AnyAsync())
        {
            await SeedTicketsAsync(db);
            return;
        }

        var email = configuration["BootstrapAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("No users exist. Configure BootstrapAdmin__Email and BootstrapAdmin__Password to create the first administrator.");
            return;
        }

        if (password.Length < 12)
        {
            throw new InvalidOperationException("Bootstrap administrator password must contain at least 12 characters.");
        }

        db.Users.Add(new User
        {
            FullName = configuration["BootstrapAdmin:FullName"]?.Trim() ?? "System Administrator",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "Admin"
        });
        await db.SaveChangesAsync();
        logger.LogInformation("Created the bootstrap administrator account.");
    }

    private static async Task EnsureDemoUsersAsync(AppDbContext db, ILogger logger)
    {
        await UpsertDemoUserAsync(db, "System Admin", "admin@helpdesk.local", "Admin@123", "Admin");
        await UpsertDemoUserAsync(db, "Support Agent", "agent@helpdesk.local", "Agent@123", "Agent");
        await UpsertDemoUserAsync(db, "Standard User", "user@helpdesk.local", "User@123", "User");
        await db.SaveChangesAsync();
        logger.LogInformation("Demo accounts are available.");
    }

    private static async Task UpsertDemoUserAsync(
        AppDbContext db,
        string fullName,
        string email,
        string password,
        string role)
    {
        var user = await db.Users.SingleOrDefaultAsync(item => item.Email == email);
        if (user is null)
        {
            db.Users.Add(new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = role
            });
            return;
        }

        user.FullName = fullName;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.Role = role;
    }

    private static async Task EnsureSchemaAsync(AppDbContext db)
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
                    "UpdatedAtUtc" TEXT NOT NULL,
                    "CreatorUserId" INTEGER NOT NULL DEFAULT 0,
                    "AssignedAgentId" INTEGER NULL
                );
                """);

            await AddColumnIfMissingAsync(db, "Tickets", "CreatorUserId", """INTEGER NOT NULL DEFAULT 0""");
            await AddColumnIfMissingAsync(db, "Tickets", "AssignedAgentId", "INTEGER NULL");

            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "TicketComments" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_TicketComments" PRIMARY KEY AUTOINCREMENT,
                    "TicketId" INTEGER NOT NULL,
                    "AuthorUserId" INTEGER NOT NULL,
                    "ParentCommentId" INTEGER NULL,
                    "Content" TEXT NOT NULL,
                    "Visibility" TEXT NOT NULL,
                    "CreatedAtUtc" TEXT NOT NULL
                );
                """);

            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "ActivityLogs" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_ActivityLogs" PRIMARY KEY AUTOINCREMENT,
                    "TicketId" INTEGER NOT NULL,
                    "ActorUserId" INTEGER NOT NULL,
                    "ActionType" TEXT NOT NULL,
                    "OldValue" TEXT NULL,
                    "NewValue" TEXT NULL,
                    "Description" TEXT NOT NULL,
                    "CreatedAtUtc" TEXT NOT NULL
                );
                """);

            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE IF NOT EXISTS "TicketStatusHistories" (
                    "Id" INTEGER NOT NULL CONSTRAINT "PK_TicketStatusHistories" PRIMARY KEY AUTOINCREMENT,
                    "TicketId" INTEGER NOT NULL,
                    "ChangedByUserId" INTEGER NOT NULL,
                    "OldStatus" TEXT NOT NULL,
                    "NewStatus" TEXT NOT NULL,
                    "ChangedAtUtc" TEXT NOT NULL
                );
                """);
        }
    }

    private static async Task AddColumnIfMissingAsync(AppDbContext db, string table, string column, string definition)
    {
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        var shouldClose = connection.State == System.Data.ConnectionState.Closed;
        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        var columnExists = false;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"PRAGMA table_info(\"{table}\");";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columnExists = string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase);
                if (columnExists)
                {
                    break;
                }
            }
        }

        if (columnExists)
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }

            return;
        }

        await using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"""ALTER TABLE "{table}" ADD COLUMN "{column}" {definition};""";
        await alterCommand.ExecuteNonQueryAsync();

        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }

    private static async Task SeedTicketsAsync(AppDbContext db)
    {
        var requester = await db.Users.FirstAsync(user => user.Role == "User");
        var agent = await db.Users.FirstOrDefaultAsync(user => user.Role == "Agent");
        await db.Database.ExecuteSqlRawAsync(
            """UPDATE "Tickets" SET "CreatorUserId" = {0} WHERE "CreatorUserId" = 0""",
            requester.Id);

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
                CreatorUserId = requester.Id,
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
                CreatorUserId = requester.Id,
                AssignedAgentId = agent?.Id,
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
                CreatorUserId = requester.Id,
                AssignedAgentId = agent?.Id,
                CreatedAtUtc = now.AddDays(-3),
                UpdatedAtUtc = now.AddDays(-1)
            }
        };

        db.Tickets.AddRange(tickets);
        await db.SaveChangesAsync();
    }
}
