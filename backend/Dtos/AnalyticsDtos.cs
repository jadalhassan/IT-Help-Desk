namespace HelpDesk.Api.Dtos;

public record DashboardStatsDto(
    int TotalProjects,
    int TotalTasks,
    int CompletedTasks,
    int PendingTasks,
    int OverdueTasks,
    int ActiveUsers,
    int UploadedFiles,
    int UnreadNotifications);

public record StatusCountDto(string Status, int Count);

public record ActivityTrendDto(DateOnly Date, int CompletedTasks, int Uploads, int Notifications);

public record RecentActivityDto(
    int Id,
    string ActionType,
    string Description,
    string? ActorName,
    int TicketId,
    DateTime CreatedAtUtc);

public record NotificationDto(
    int Id,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    string? RelatedEntityType,
    string? RelatedEntityId,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);

public record CreateNotificationRequest(
    int? UserId,
    string Title,
    string Message,
    string Type,
    string? RelatedEntityType,
    string? RelatedEntityId);

public record AttachmentDto(
    int Id,
    string FileName,
    string OriginalFileName,
    string ContentType,
    long Size,
    string Url,
    string UploadedBy,
    DateTime UploadedAt,
    string RelatedEntityType,
    string RelatedEntityId,
    string? Description);
