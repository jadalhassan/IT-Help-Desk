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
public class AttachmentsController(
    AppDbContext db,
    IWebHostEnvironment environment,
    INotificationService notificationService,
    IUploadValidationService uploadValidation) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AttachmentDto>>> GetAttachments(
        [FromQuery] string relatedEntityType,
        [FromQuery] string relatedEntityId)
    {
        if (!await CanAccessEntityAsync(relatedEntityType, relatedEntityId))
        {
            return Forbid();
        }

        var attachments = await db.Attachments.AsNoTracking()
            .Include(attachment => attachment.UploadedByUser)
            .Where(attachment => attachment.RelatedEntityType == NormalizeEntityType(relatedEntityType) &&
                attachment.RelatedEntityId == relatedEntityId)
            .OrderByDescending(attachment => attachment.UploadedAtUtc)
            .Select(attachment => ToDto(attachment))
            .ToListAsync();

        return Ok(attachments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AttachmentDto>> GetAttachment(int id)
    {
        var attachment = await db.Attachments.AsNoTracking()
            .Include(item => item.UploadedByUser)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (attachment is null)
        {
            return NotFound();
        }

        if (!await CanAccessEntityAsync(attachment.RelatedEntityType, attachment.RelatedEntityId))
        {
            return Forbid();
        }

        return Ok(ToDto(attachment));
    }

    [HttpPost("upload")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<ActionResult<AttachmentDto>> Upload(
        IFormFile file,
        [FromForm] string relatedEntityType,
        [FromForm] string relatedEntityId,
        [FromForm] string? description)
    {
        var entityType = NormalizeEntityType(relatedEntityType);
        var validationError = await uploadValidation.ValidateAsync(file, HttpContext.RequestAborted);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        if (!await CanAccessEntityAsync(entityType, relatedEntityId))
        {
            return Forbid();
        }

        var uploadRoot = Path.Combine(environment.ContentRootPath, "Uploads");
        Directory.CreateDirectory(uploadRoot);

        var extension = Path.GetExtension(file.FileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var storagePath = Path.Combine(uploadRoot, storedFileName);

        await using (var stream = System.IO.File.Create(storagePath))
        {
            await file.CopyToAsync(stream);
        }

        var now = DateTime.UtcNow;
        var attachment = new Attachment
        {
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            StoragePath = storagePath,
            RelatedEntityType = entityType,
            RelatedEntityId = relatedEntityId,
            UploadedByUserId = CurrentUserId(),
            UploadedAtUtc = now,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };

        db.Attachments.Add(attachment);
        await AddUploadActivityAsync(entityType, relatedEntityId, now);
        await db.SaveChangesAsync();
        await db.Entry(attachment).Reference(item => item.UploadedByUser).LoadAsync();

        await NotifyAttachmentUploadAsync(attachment);

        return CreatedAtAction(nameof(GetAttachment), new { id = attachment.Id }, ToDto(attachment));
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var attachment = await db.Attachments.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (attachment is null)
        {
            return NotFound();
        }

        if (!await CanAccessEntityAsync(attachment.RelatedEntityType, attachment.RelatedEntityId))
        {
            return Forbid();
        }

        var fullPath = Path.GetFullPath(attachment.StoragePath);
        var uploadRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "Uploads"));
        if (!fullPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
        {
            return NotFound(new { message = "The uploaded file could not be found." });
        }

        var stream = System.IO.File.OpenRead(fullPath);
        return File(stream, attachment.ContentType, attachment.OriginalFileName);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAttachment(int id)
    {
        var attachment = await db.Attachments.FirstOrDefaultAsync(item => item.Id == id);
        if (attachment is null)
        {
            return NotFound();
        }

        if (!await CanAccessEntityAsync(attachment.RelatedEntityType, attachment.RelatedEntityId))
        {
            return Forbid();
        }

        if (CurrentRole() != "Admin" && attachment.UploadedByUserId != CurrentUserId())
        {
            return Forbid();
        }

        var fullPath = Path.GetFullPath(attachment.StoragePath);
        var uploadRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "Uploads"));
        if (fullPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }

        db.Attachments.Remove(attachment);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private async Task AddUploadActivityAsync(string entityType, string entityId, DateTime now)
    {
        if (entityType != "ticket" || !int.TryParse(entityId, out var ticketId))
        {
            return;
        }

        var ticket = await db.Tickets.FindAsync(ticketId);
        if (ticket is null)
        {
            return;
        }

        ticket.UpdatedAtUtc = now;
        db.ActivityLogs.Add(new ActivityLog
        {
            TicketId = ticketId,
            ActorUserId = CurrentUserId(),
            ActionType = "FileUploaded",
            Description = "Attachment uploaded.",
            CreatedAtUtc = now
        });
    }

    private async Task NotifyAttachmentUploadAsync(Attachment attachment)
    {
        if (attachment.RelatedEntityType != "ticket" || !int.TryParse(attachment.RelatedEntityId, out var ticketId))
        {
            return;
        }

        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(item => item.Id == ticketId);
        if (ticket is null)
        {
            return;
        }

        var recipients = new[] { ticket.CreatorUserId, ticket.AssignedAgentId }
            .Where(id => id.HasValue && id.Value != CurrentUserId())
            .Select(id => id!.Value)
            .Distinct();

        foreach (var userId in recipients)
        {
            await notificationService.CreateForUserAsync(
                userId,
                "File uploaded",
                $"{attachment.OriginalFileName} was added to ticket #{ticket.Id}.",
                "info",
                "ticket",
                ticket.Id.ToString(),
                HttpContext.RequestAborted);
        }
    }

    private async Task<bool> CanAccessEntityAsync(string relatedEntityType, string relatedEntityId)
    {
        var entityType = NormalizeEntityType(relatedEntityType);
        if (entityType != "ticket")
        {
            return CurrentRole() == "Admin";
        }

        if (!int.TryParse(relatedEntityId, out var ticketId))
        {
            return false;
        }

        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(item => item.Id == ticketId);
        if (ticket is null)
        {
            return false;
        }

        var currentUserId = CurrentUserId();
        return CurrentRole() switch
        {
            "Admin" => true,
            "Agent" => ticket.AssignedAgentId == currentUserId || ticket.AssignedAgentId == null,
            _ => ticket.CreatorUserId == currentUserId
        };
    }

    private static string NormalizeEntityType(string value) => value.Trim().ToLowerInvariant();

    private int CurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }

    private string CurrentRole() => User.FindFirstValue(ClaimTypes.Role) ?? "User";

    private static AttachmentDto ToDto(Attachment attachment) => new(
        attachment.Id,
        attachment.StoredFileName,
        attachment.OriginalFileName,
        attachment.ContentType,
        attachment.FileSize,
        $"/api/attachments/{attachment.Id}/download",
        attachment.UploadedByUser?.FullName ?? "Unknown",
        attachment.UploadedAtUtc,
        attachment.RelatedEntityType,
        attachment.RelatedEntityId,
        attachment.Description);
}
