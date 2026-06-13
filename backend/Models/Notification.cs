using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Models;

public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Type { get; set; } = "info";

    public bool IsRead { get; set; }

    [MaxLength(64)]
    public string? RelatedEntityType { get; set; }

    [MaxLength(64)]
    public string? RelatedEntityId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAtUtc { get; set; }
}
