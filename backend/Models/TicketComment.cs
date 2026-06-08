using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Models;

public class TicketComment
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public Ticket? Ticket { get; set; }

    public int AuthorUserId { get; set; }

    public User? AuthorUser { get; set; }

    public int? ParentCommentId { get; set; }

    public TicketComment? ParentComment { get; set; }

    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Visibility { get; set; } = "Public";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
