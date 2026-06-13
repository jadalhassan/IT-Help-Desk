using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Data;

public static class SchemaInitializer
{
    public static async Task EnsureAnalyticsSchemaAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Notifications" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Notifications" PRIMARY KEY AUTOINCREMENT,
                "UserId" INTEGER NOT NULL,
                "Title" TEXT NOT NULL,
                "Message" TEXT NOT NULL,
                "Type" TEXT NOT NULL,
                "IsRead" INTEGER NOT NULL,
                "RelatedEntityType" TEXT NULL,
                "RelatedEntityId" TEXT NULL,
                "CreatedAtUtc" TEXT NOT NULL,
                "ReadAtUtc" TEXT NULL,
                CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Attachments" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Attachments" PRIMARY KEY AUTOINCREMENT,
                "OriginalFileName" TEXT NOT NULL,
                "StoredFileName" TEXT NOT NULL,
                "ContentType" TEXT NOT NULL,
                "FileSize" INTEGER NOT NULL,
                "StoragePath" TEXT NOT NULL,
                "RelatedEntityType" TEXT NOT NULL,
                "RelatedEntityId" TEXT NOT NULL,
                "UploadedByUserId" INTEGER NOT NULL,
                "UploadedAtUtc" TEXT NOT NULL,
                "Description" TEXT NULL,
                CONSTRAINT "FK_Attachments_Users_UploadedByUserId" FOREIGN KEY ("UploadedByUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
            );
            """);

        await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Notifications_UserId_IsRead\" ON \"Notifications\" (\"UserId\", \"IsRead\");");
        await db.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Attachments_RelatedEntityType_RelatedEntityId\" ON \"Attachments\" (\"RelatedEntityType\", \"RelatedEntityId\");");
    }
}
