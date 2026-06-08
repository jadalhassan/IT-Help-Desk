using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Models;

public class Ticket
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Priority { get; set; } = "Medium";

    [MaxLength(32)]
    public string Status { get; set; } = "Open";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public int CreatorUserId { get; set; }

    public User? CreatorUser { get; set; }

    public int? AssignedAgentId { get; set; }

    public User? AssignedAgent { get; set; }

    public List<TicketComment> Comments { get; set; } = [];

    public List<ActivityLog> ActivityLogs { get; set; } = [];

    public List<TicketStatusHistory> StatusHistory { get; set; } = [];
}
