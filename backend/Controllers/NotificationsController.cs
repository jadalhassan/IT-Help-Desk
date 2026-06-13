using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HelpDesk.Api.Data;
using HelpDesk.Api.Dtos;
using HelpDesk.Api.Models;
using HelpDesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController(AppDbContext db, INotificationService notificationService) : ControllerBase
{
    private static readonly string[] Types = ["info", "success", "warning", "error"];

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetNotifications()
    {
        var notifications = await db.Notifications.AsNoTracking()
            .Where(notification => notification.UserId == CurrentUserId())
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .Take(50)
            .Select(notification => ToDto(notification))
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<object>> GetUnreadCount()
    {
        var count = await db.Notifications.CountAsync(notification =>
            notification.UserId == CurrentUserId() && !notification.IsRead);

        return Ok(new { count });
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<NotificationDto>> CreateNotification(CreateNotificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Notification title and message are required." });
        }

        var targetUserIds = request.UserId.HasValue
            ? new List<int> { request.UserId.Value }
            : await db.Users.Select(user => user.Id).ToListAsync();

        var created = new List<NotificationDto>();
        foreach (var userId in targetUserIds)
        {
            created.Add(await notificationService.CreateForUserAsync(
                userId,
                request.Title,
                request.Message,
                NormalizeType(request.Type),
                request.RelatedEntityType,
                request.RelatedEntityId,
                HttpContext.RequestAborted));
        }

        return Ok(created.FirstOrDefault());
    }

    [HttpPatch("{id:int}/read")]
    public async Task<ActionResult<NotificationDto>> MarkAsRead(int id)
    {
        var notification = await db.Notifications.FirstOrDefaultAsync(item =>
            item.Id == id && item.UserId == CurrentUserId());

        if (notification is null)
        {
            return NotFound();
        }

        notification.IsRead = true;
        notification.ReadAtUtc ??= DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(ToDto(notification));
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var now = DateTime.UtcNow;
        await db.Notifications
            .Where(notification => notification.UserId == CurrentUserId() && !notification.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(notification => notification.IsRead, true)
                .SetProperty(notification => notification.ReadAtUtc, (DateTime?)now));

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var deleted = await db.Notifications
            .Where(notification => notification.Id == id && notification.UserId == CurrentUserId())
            .ExecuteDeleteAsync();

        return deleted == 0 ? NotFound() : NoContent();
    }

    private static string NormalizeType(string type)
    {
        var normalized = type.Trim().ToLowerInvariant();
        return Types.Contains(normalized) ? normalized : "info";
    }

    private int CurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
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
