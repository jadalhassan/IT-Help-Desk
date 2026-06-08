using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Models;

public class ActivityLog
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public Ticket? Ticket { get; set; }

    public int ActorUserId { get; set; }

    public User? ActorUser { get; set; }

    [MaxLength(64)]
    public string ActionType { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? OldValue { get; set; }

    [MaxLength(512)]
    public string? NewValue { get; set; }

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
