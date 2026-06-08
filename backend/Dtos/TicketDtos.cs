using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Dtos;

public record TicketDto(
    int Id,
    string Title,
    string Description,
    string Category,
    string Priority,
    string Status,
    int CreatorUserId,
    string? CreatorName,
    int? AssignedAgentId,
    string? AssignedAgentName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<TicketCommentDto> Comments,
    IReadOnlyList<ActivityLogDto> ActivityLogs,
    IReadOnlyList<TicketStatusHistoryDto> StatusHistory);

public record UserSummaryDto(int Id, string FullName, string Email, string Role);

public record TicketCommentDto(
    int Id,
    int TicketId,
    int AuthorUserId,
    string? AuthorName,
    int? ParentCommentId,
    string Content,
    string Visibility,
    DateTime CreatedAtUtc);

public record ActivityLogDto(
    int Id,
    int TicketId,
    int ActorUserId,
    string? ActorName,
    string ActionType,
    string? OldValue,
    string? NewValue,
    string Description,
    DateTime CreatedAtUtc);

public record TicketStatusHistoryDto(
    int Id,
    int TicketId,
    int ChangedByUserId,
    string? ChangedByName,
    string OldStatus,
    string NewStatus,
    DateTime ChangedAtUtc);

public class TicketRequest
{
    [Required]
    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string Priority { get; set; } = "Medium";

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = "Open";
}

public class AssignTicketRequest
{
    [Required]
    public int AgentUserId { get; set; }
}

public class UpdateTicketStatusRequest
{
    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = string.Empty;
}

public class AddTicketCommentRequest
{
    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Visibility { get; set; } = "Public";

    public int? ParentCommentId { get; set; }
}
