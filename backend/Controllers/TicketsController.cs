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
public class TicketsController(
    AppDbContext db,
    INotificationService notificationService,
    IWebHostEnvironment environment) : ControllerBase
{
    private static readonly string[] CommentVisibilities = ["Public", "Internal"];

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetTickets(
        [FromQuery] string? category,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] int? assignedAgentId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var currentUserId = CurrentUserId();
        var role = CurrentRole();
        var tickets = IncludeTicketDetails(db.Tickets.AsNoTracking());

        tickets = role switch
        {
            "Admin" => tickets,
            "Agent" => tickets.Where(ticket => ticket.AssignedAgentId == currentUserId || ticket.AssignedAgentId == null),
            _ => tickets.Where(ticket => ticket.CreatorUserId == currentUserId)
        };

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            tickets = tickets.Where(ticket => ticket.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            tickets = tickets.Where(ticket => ticket.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(priority) && priority != "All")
        {
            tickets = tickets.Where(ticket => ticket.Priority == priority);
        }

        if (assignedAgentId.HasValue)
        {
            tickets = tickets.Where(ticket => ticket.AssignedAgentId == assignedAgentId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            tickets = tickets.Where(ticket => ticket.Title.Contains(term) || ticket.Description.Contains(term));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await tickets.CountAsync();
        Response.Headers["X-Pagination"] = System.Text.Json.JsonSerializer.Serialize(new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });

        var ticketList = await tickets
            .OrderByDescending(ticket => ticket.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var result = ticketList.Select(ticket => ToDto(ticket, role));

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDto>> GetTicket(int id)
    {
        var ticket = await IncludeTicketDetails(db.Tickets.AsNoTracking())
            .FirstOrDefaultAsync(ticket => ticket.Id == id);

        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanView(ticket))
        {
            return Forbid();
        }

        return Ok(ToDto(ticket, CurrentRole()));
    }

    [HttpPost]
    public async Task<ActionResult<TicketDto>> CreateTicket(TicketRequest request)
    {
        var validationError = ValidateTicketRequest(request, allowStatus: false);
        if (validationError is not null)
        {
            return validationError;
        }

        var now = DateTime.UtcNow;
        var actorId = CurrentUserId();
        var ticket = new Ticket
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category,
            Priority = request.Priority,
            Status = "Open",
            CreatorUserId = actorId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        AddActivity(ticket.Id, actorId, "TicketCreated", null, "Open", "Ticket created.", now);
        await db.SaveChangesAsync();

        var admins = await db.Users.AsNoTracking()
            .Where(user => user.Role == "Admin" && user.Id != actorId)
            .Select(user => user.Id)
            .ToListAsync();
        foreach (var adminId in admins)
        {
            await notificationService.CreateForUserAsync(
                adminId,
                "New ticket created",
                $"Ticket #{ticket.Id} was created: {ticket.Title}.",
                "info",
                "ticket",
                ticket.Id.ToString(),
                HttpContext.RequestAborted);
        }

        var dto = ToDto(await LoadTicketAsync(ticket.Id), CurrentRole());
        return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TicketDto>> UpdateTicket(int id, TicketRequest request)
    {
        var validationError = ValidateTicketRequest(request, allowStatus: true);
        if (validationError is not null)
        {
            return validationError;
        }

        var ticket = await db.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanEditTicket(ticket))
        {
            return Forbid();
        }

        var actorId = CurrentUserId();
        var now = DateTime.UtcNow;
        var oldPriority = ticket.Priority;

        ticket.Title = request.Title.Trim();
        ticket.Description = request.Description.Trim();
        ticket.Category = request.Category;
        ticket.Priority = request.Priority;
        ticket.UpdatedAtUtc = now;

        if (oldPriority != ticket.Priority)
        {
            AddActivity(ticket.Id, actorId, "PriorityChanged", oldPriority, ticket.Priority, "Ticket priority changed.", now);
        }

        await db.SaveChangesAsync();

        return Ok(ToDto(await LoadTicketAsync(id), CurrentRole()));
    }

    [HttpPost("{id:int}/assign")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<TicketDto>> AssignTicket(int id, AssignTicketRequest request)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        var agent = await db.Users.FindAsync(request.AgentUserId);
        if (agent is null || agent.Role != "Agent")
        {
            return BadRequest(new { message = "Selected user is not an agent." });
        }

        var actorId = CurrentUserId();
        var now = DateTime.UtcNow;
        var oldAgent = ticket.AssignedAgentId?.ToString();
        ticket.AssignedAgentId = agent.Id;
        ticket.UpdatedAtUtc = now;

        AddActivity(ticket.Id, actorId, "TicketAssigned", oldAgent, agent.Id.ToString(), $"Ticket assigned to {agent.FullName}.", now);

        if (ticket.Status == "Open")
        {
            AddStatusChange(ticket, actorId, "Assigned", now);
        }

        await db.SaveChangesAsync();

        await notificationService.CreateForUserAsync(
            agent.Id,
            "Ticket assigned",
            $"Ticket #{ticket.Id} was assigned to you.",
            "info",
            "ticket",
            ticket.Id.ToString(),
            HttpContext.RequestAborted);

        return Ok(ToDto(await LoadTicketAsync(id), CurrentRole()));
    }

    [HttpPost("{id:int}/claim")]
    [Authorize(Policy = "AgentOrAdmin")]
    public async Task<ActionResult<TicketDto>> ClaimTicket(int id)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (CurrentRole() == "Agent" && ticket.AssignedAgentId.HasValue && ticket.AssignedAgentId != CurrentUserId())
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Ticket already assigned",
                Detail = "Another agent has already claimed this ticket."
            });
        }

        var agentId = CurrentRole() == "Agent" ? CurrentUserId() : ticket.AssignedAgentId;
        if (!agentId.HasValue)
        {
            return BadRequest(new { message = "Select an agent before claiming as an administrator." });
        }

        var now = DateTime.UtcNow;
        ticket.AssignedAgentId = agentId.Value;
        ticket.UpdatedAtUtc = now;
        AddActivity(ticket.Id, CurrentUserId(), "TicketClaimed", null, agentId.Value.ToString(), "Ticket claimed by an agent.", now);
        if (ticket.Status == "Open")
        {
            AddStatusChange(ticket, CurrentUserId(), "Assigned", now);
        }

        await db.SaveChangesAsync();
        return Ok(ToDto(await LoadTicketAsync(id), CurrentRole()));
    }

    [HttpPost("{id:int}/status")]
    [Authorize(Policy = "AgentOrAdmin")]
    public async Task<ActionResult<TicketDto>> UpdateStatus(int id, UpdateTicketStatusRequest request)
    {
        if (!TicketWorkflow.Statuses.Contains(request.Status))
        {
            return BadRequest(new { message = "Invalid ticket status." });
        }

        var ticket = await db.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (CurrentRole() == "Agent" && ticket.AssignedAgentId != CurrentUserId())
        {
            return Forbid();
        }

        if (!TicketWorkflow.CanTransition(ticket.Status, request.Status))
        {
            return BadRequest(new { message = $"Cannot change status from {ticket.Status} to {request.Status}." });
        }

        var oldStatus = ticket.Status;
        AddStatusChange(ticket, CurrentUserId(), request.Status, DateTime.UtcNow);
        await db.SaveChangesAsync();

        if (oldStatus != request.Status)
        {
            var recipients = new[] { ticket.CreatorUserId, ticket.AssignedAgentId }
                .Where(userId => userId.HasValue && userId.Value != CurrentUserId())
                .Select(userId => userId!.Value)
                .Distinct();

            foreach (var userId in recipients)
            {
                await notificationService.CreateForUserAsync(
                    userId,
                    "Ticket status updated",
                    $"Ticket #{ticket.Id} changed from {oldStatus} to {request.Status}.",
                    request.Status is "Resolved" or "Closed" ? "success" : "info",
                    "ticket",
                    ticket.Id.ToString(),
                    HttpContext.RequestAborted);
            }
        }

        return Ok(ToDto(await LoadTicketAsync(id), CurrentRole()));
    }

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<TicketCommentDto>> AddComment(int id, AddTicketCommentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { message = "Comment content is required." });
        }

        if (!CommentVisibilities.Contains(request.Visibility))
        {
            return BadRequest(new { message = "Invalid comment visibility." });
        }

        if (request.Visibility == "Internal" && !IsStaff())
        {
            return Forbid();
        }

        var ticket = await db.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanView(ticket))
        {
            return Forbid();
        }

        if (request.ParentCommentId.HasValue &&
            !await db.TicketComments.AnyAsync(comment => comment.Id == request.ParentCommentId && comment.TicketId == id))
        {
            return BadRequest(new { message = "Parent comment does not belong to this ticket." });
        }

        var now = DateTime.UtcNow;
        var actorId = CurrentUserId();
        var comment = new TicketComment
        {
            TicketId = id,
            AuthorUserId = actorId,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content.Trim(),
            Visibility = request.Visibility,
            CreatedAtUtc = now
        };

        ticket.UpdatedAtUtc = now;
        db.TicketComments.Add(comment);
        AddActivity(
            id,
            actorId,
            request.Visibility == "Internal" ? "InternalNoteAdded" : "CommentAdded",
            null,
            request.Visibility,
            request.Visibility == "Internal" ? "Internal note added." : "Comment added.",
            now);

        await db.SaveChangesAsync();
        await db.Entry(comment).Reference(x => x.AuthorUser).LoadAsync();

        return CreatedAtAction(nameof(GetTicket), new { id }, ToCommentDto(comment));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteTicket(int id)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        await db.TicketComments.Where(comment => comment.TicketId == id).ExecuteDeleteAsync();
        await db.ActivityLogs.Where(log => log.TicketId == id).ExecuteDeleteAsync();
        await db.TicketStatusHistories.Where(history => history.TicketId == id).ExecuteDeleteAsync();
        await DeleteTicketAttachmentsAsync(id);
        await db.Notifications
            .Where(notification => notification.RelatedEntityType == "ticket" && notification.RelatedEntityId == id.ToString())
            .ExecuteDeleteAsync();
        db.Tickets.Remove(ticket);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private async Task DeleteTicketAttachmentsAsync(int ticketId)
    {
        var attachments = await db.Attachments
            .Where(attachment => attachment.RelatedEntityType == "ticket" && attachment.RelatedEntityId == ticketId.ToString())
            .ToListAsync();

        var uploadRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "Uploads"));
        foreach (var attachment in attachments)
        {
            var fullPath = Path.GetFullPath(attachment.StoragePath);
            if (fullPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        db.Attachments.RemoveRange(attachments);
    }

    [HttpGet("/api/categories")]
    [AllowAnonymous]
    public ActionResult<IEnumerable<string>> GetCategories() => Ok(TicketWorkflow.Categories);

    [HttpGet("/api/statuses")]
    [AllowAnonymous]
    public ActionResult<IEnumerable<string>> GetStatuses() => Ok(TicketWorkflow.Statuses);

    [HttpGet("/api/users/agents")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetAgents()
    {
        var agents = await db.Users.AsNoTracking()
            .Where(user => user.Role == "Agent")
            .OrderBy(user => user.FullName)
            .Select(user => new UserSummaryDto(user.Id, user.FullName, user.Email, user.Role))
            .ToListAsync();

        return Ok(agents);
    }

    private static IQueryable<Ticket> IncludeTicketDetails(IQueryable<Ticket> tickets) => tickets
        .Include(ticket => ticket.CreatorUser)
        .Include(ticket => ticket.AssignedAgent)
        .Include(ticket => ticket.Comments).ThenInclude(comment => comment.AuthorUser)
        .Include(ticket => ticket.ActivityLogs).ThenInclude(log => log.ActorUser)
        .Include(ticket => ticket.StatusHistory).ThenInclude(history => history.ChangedByUser)
        .AsSplitQuery();

    private async Task<Ticket> LoadTicketAsync(int id)
    {
        return await IncludeTicketDetails(db.Tickets)
            .FirstAsync(ticket => ticket.Id == id);
    }

    private bool CanView(Ticket ticket)
    {
        var currentUserId = CurrentUserId();
        return CurrentRole() switch
        {
            "Admin" => true,
            "Agent" => ticket.AssignedAgentId == currentUserId || ticket.AssignedAgentId == null,
            _ => ticket.CreatorUserId == currentUserId
        };
    }

    private bool CanEditTicket(Ticket ticket)
    {
        var currentUserId = CurrentUserId();
        return CurrentRole() switch
        {
            "Admin" => true,
            "Agent" => ticket.AssignedAgentId == currentUserId,
            _ => ticket.CreatorUserId == currentUserId && ticket.Status is "Open" or "Waiting for User"
        };
    }

    private bool IsStaff() => CurrentRole() is "Admin" or "Agent";

    private void AddStatusChange(Ticket ticket, int actorId, string newStatus, DateTime now)
    {
        var oldStatus = ticket.Status;
        if (oldStatus == newStatus)
        {
            return;
        }

        ticket.Status = newStatus;
        ticket.UpdatedAtUtc = now;
        db.TicketStatusHistories.Add(new TicketStatusHistory
        {
            TicketId = ticket.Id,
            ChangedByUserId = actorId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedAtUtc = now
        });
        AddActivity(ticket.Id, actorId, "StatusChanged", oldStatus, newStatus, $"Ticket status changed from {oldStatus} to {newStatus}.", now);
    }

    private void AddActivity(int ticketId, int actorId, string actionType, string? oldValue, string? newValue, string description, DateTime now)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            TicketId = ticketId,
            ActorUserId = actorId,
            ActionType = actionType,
            OldValue = oldValue,
            NewValue = newValue,
            Description = description,
            CreatedAtUtc = now
        });
    }

    private static BadRequestObjectResult? ValidateTicketRequest(TicketRequest request, bool allowStatus)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
        {
            return new BadRequestObjectResult(new { message = "Title and description are required." });
        }

        if (!TicketWorkflow.Categories.Contains(request.Category))
        {
            return new BadRequestObjectResult(new { message = "Invalid ticket category." });
        }

        if (!TicketWorkflow.Priorities.Contains(request.Priority))
        {
            return new BadRequestObjectResult(new { message = "Invalid ticket priority." });
        }

        if (allowStatus && !TicketWorkflow.Statuses.Contains(request.Status))
        {
            return new BadRequestObjectResult(new { message = "Invalid ticket status." });
        }

        return null;
    }

    private int CurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }

    private string CurrentRole() => User.FindFirstValue(ClaimTypes.Role) ?? "User";

    private static TicketDto ToDto(Ticket ticket, string currentRole) => new(
        ticket.Id,
        ticket.Title,
        ticket.Description,
        ticket.Category,
        ticket.Priority,
        ticket.Status,
        ticket.CreatorUserId,
        ticket.CreatorUser?.FullName,
        ticket.AssignedAgentId,
        ticket.AssignedAgent?.FullName,
        ticket.CreatedAtUtc,
        ticket.UpdatedAtUtc,
        ticket.Comments
            .Where(comment => currentRole is "Admin" or "Agent" || comment.Visibility == "Public")
            .OrderBy(comment => comment.CreatedAtUtc)
            .Select(ToCommentDto)
            .ToList(),
        ticket.ActivityLogs
            .Where(log => currentRole is "Admin" or "Agent" || log.ActionType != "InternalNoteAdded")
            .OrderBy(log => log.CreatedAtUtc)
            .Select(ToActivityDto)
            .ToList(),
        ticket.StatusHistory
            .OrderBy(history => history.ChangedAtUtc)
            .Select(ToStatusHistoryDto)
            .ToList());

    private static TicketCommentDto ToCommentDto(TicketComment comment) => new(
        comment.Id,
        comment.TicketId,
        comment.AuthorUserId,
        comment.AuthorUser?.FullName,
        comment.ParentCommentId,
        comment.Content,
        comment.Visibility,
        comment.CreatedAtUtc);

    private static ActivityLogDto ToActivityDto(ActivityLog log) => new(
        log.Id,
        log.TicketId,
        log.ActorUserId,
        log.ActorUser?.FullName,
        log.ActionType,
        log.OldValue,
        log.NewValue,
        log.Description,
        log.CreatedAtUtc);

    private static TicketStatusHistoryDto ToStatusHistoryDto(TicketStatusHistory history) => new(
        history.Id,
        history.TicketId,
        history.ChangedByUserId,
        history.ChangedByUser?.FullName,
        history.OldStatus,
        history.NewStatus,
        history.ChangedAtUtc);
}
