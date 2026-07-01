using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HelpDesk.Api.Data;
using HelpDesk.Api.Dtos;
using HelpDesk.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var tickets = VisibleTickets();
        var unresolved = tickets.Where(ticket => ticket.Status != "Resolved" && ticket.Status != "Closed");
        var overdueCutoff = DateTime.UtcNow.AddDays(-3);

        var stats = new DashboardStatsDto(
            await tickets.Select(ticket => ticket.Category).Distinct().CountAsync(),
            await tickets.CountAsync(),
            await tickets.CountAsync(ticket => ticket.Status == "Resolved" || ticket.Status == "Closed"),
            await unresolved.CountAsync(),
            await unresolved.CountAsync(ticket => ticket.CreatedAtUtc < overdueCutoff),
            await db.Users.CountAsync(),
            await VisibleAttachments().CountAsync(),
            await db.Notifications.CountAsync(notification => notification.UserId == CurrentUserId() && !notification.IsRead));

        return Ok(stats);
    }

    [HttpGet("charts/tasks-by-status")]
    public async Task<ActionResult<IEnumerable<StatusCountDto>>> GetTasksByStatus()
    {
        var tickets = await VisibleTickets()
            .Select(ticket => ticket.Status)
            .ToListAsync();

        var result = tickets
            .GroupBy(status => status)
            .Select(group => new StatusCountDto(group.Key, group.Count()))
            .OrderBy(item => item.Status)
            .ToList();

        return Ok(result);
    }

    [HttpGet("charts/activity-trends")]
    public async Task<ActionResult<IEnumerable<ActivityTrendDto>>> GetActivityTrends()
    {
        var from = DateTime.UtcNow.Date.AddDays(-13);
        var role = CurrentRole();
        var userId = CurrentUserId();
        var visibleTicketIds = await VisibleTickets().Select(ticket => ticket.Id).ToListAsync();
        var visibleTicketIdStrings = visibleTicketIds.Select(id => id.ToString()).ToList();

        var statusHistory = await db.TicketStatusHistories.AsNoTracking()
            .Where(history => visibleTicketIds.Contains(history.TicketId) &&
                history.ChangedAtUtc >= from &&
                (history.NewStatus == "Resolved" || history.NewStatus == "Closed"))
            .Select(history => history.ChangedAtUtc)
            .ToListAsync();

        var uploads = await db.Attachments.AsNoTracking()
            .Where(attachment => attachment.UploadedAtUtc >= from &&
                (role == "Admin" ||
                    (attachment.RelatedEntityType == "ticket" && visibleTicketIdStrings.Contains(attachment.RelatedEntityId)) ||
                    attachment.UploadedByUserId == userId))
            .Select(attachment => attachment.UploadedAtUtc)
            .ToListAsync();

        var notifications = await db.Notifications.AsNoTracking()
            .Where(notification => notification.UserId == userId && notification.CreatedAtUtc >= from)
            .Select(notification => notification.CreatedAtUtc)
            .ToListAsync();

        var result = Enumerable.Range(0, 14)
            .Select(offset => DateOnly.FromDateTime(from.AddDays(offset)))
            .Select(day => new ActivityTrendDto(
                day,
                statusHistory.Count(value => DateOnly.FromDateTime(value) == day),
                uploads.Count(value => DateOnly.FromDateTime(value) == day),
                notifications.Count(value => DateOnly.FromDateTime(value) == day)))
            .ToList();

        return Ok(result);
    }

    [HttpGet("recent-activity")]
    public async Task<ActionResult<IEnumerable<RecentActivityDto>>> GetRecentActivity()
    {
        var visibleTicketIds = await VisibleTickets().Select(ticket => ticket.Id).ToListAsync();
        var result = await VisibleActivityLogs(db.ActivityLogs.AsNoTracking())
            .Include(log => log.ActorUser)
            .Where(log => visibleTicketIds.Contains(log.TicketId))
            .OrderByDescending(log => log.CreatedAtUtc)
            .Take(10)
            .Select(log => new RecentActivityDto(
                log.Id,
                log.ActionType,
                log.Description,
                log.ActorUser == null ? null : log.ActorUser.FullName,
                log.TicketId,
                log.CreatedAtUtc))
            .ToListAsync();

        return Ok(result);
    }

    private IQueryable<ActivityLog> VisibleActivityLogs(IQueryable<ActivityLog> logs)
    {
        return CurrentRole() is "Admin" or "Agent"
            ? logs
            : logs.Where(log => log.ActionType != "InternalNoteAdded");
    }

    private IQueryable<Ticket> VisibleTickets()
    {
        var currentUserId = CurrentUserId();
        return CurrentRole() switch
        {
            "Admin" => db.Tickets.AsNoTracking(),
            "Agent" => db.Tickets.AsNoTracking().Where(ticket => ticket.AssignedAgentId == currentUserId || ticket.AssignedAgentId == null),
            _ => db.Tickets.AsNoTracking().Where(ticket => ticket.CreatorUserId == currentUserId)
        };
    }

    private IQueryable<Attachment> VisibleAttachments()
    {
        if (CurrentRole() == "Admin")
        {
            return db.Attachments.AsNoTracking();
        }

        var visibleTicketIds = VisibleTickets().Select(ticket => ticket.Id.ToString());
        return db.Attachments.AsNoTracking()
            .Where(attachment => attachment.UploadedByUserId == CurrentUserId() ||
                (attachment.RelatedEntityType == "ticket" && visibleTicketIds.Contains(attachment.RelatedEntityId)));
    }

    private int CurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }

    private string CurrentRole() => User.FindFirstValue(ClaimTypes.Role) ?? "User";
}
