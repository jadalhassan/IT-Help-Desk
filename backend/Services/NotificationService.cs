using HelpDesk.Api.Data;
using HelpDesk.Api.Dtos;
using HelpDesk.Api.Hubs;
using HelpDesk.Api.Models;
using Microsoft.AspNetCore.SignalR;

namespace HelpDesk.Api.Services;

public class NotificationService(AppDbContext db, IHubContext<NotificationHub> hub) : INotificationService
{
    public async Task<NotificationDto> CreateForUserAsync(
        int userId,
        string title,
        string message,
        string type = "info",
        string? relatedEntityType = null,
        string? relatedEntityId = null,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title.Trim(),
            Message = message.Trim(),
            Type = NormalizeType(type),
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);

        var dto = ToDto(notification);
        await hub.Clients.Group(NotificationHub.UserGroup(userId.ToString()))
            .SendAsync("ReceiveNotification", dto, cancellationToken);

        return dto;
    }

    private static string NormalizeType(string type)
    {
        var normalized = type.Trim().ToLowerInvariant();
        return normalized is "info" or "success" or "warning" or "error" ? normalized : "info";
    }

    private static NotificationDto ToDto(Notification notification) => new(
        notification.Id,
        notification.Title,
        notification.Message,
        notification.Type,
        notification.IsRead,
        notification.RelatedEntityType,
        notification.RelatedEntityId,
        notification.CreatedAtUtc,
        notification.ReadAtUtc);
}
