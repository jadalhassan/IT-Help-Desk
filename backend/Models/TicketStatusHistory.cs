using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Models;

public class TicketStatusHistory
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public Ticket? Ticket { get; set; }

    public int ChangedByUserId { get; set; }

    public User? ChangedByUser { get; set; }

    [MaxLength(32)]
    public string OldStatus { get; set; } = string.Empty;

    [MaxLength(32)]
    public string NewStatus { get; set; } = string.Empty;

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
