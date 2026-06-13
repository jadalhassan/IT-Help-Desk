using HelpDesk.Api.Dtos;

namespace HelpDesk.Api.Services;

public interface INotificationService
{
    Task<NotificationDto> CreateForUserAsync(
        int userId,
        string title,
        string message,
        string type = "info",
        string? relatedEntityType = null,
        string? relatedEntityId = null,
        CancellationToken cancellationToken = default);
}
