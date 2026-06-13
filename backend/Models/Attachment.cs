using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Models;

public class Attachment
{
    public int Id { get; set; }

    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [MaxLength(128)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [MaxLength(512)]
    public string StoragePath { get; set; } = string.Empty;

    [MaxLength(64)]
    public string RelatedEntityType { get; set; } = string.Empty;

    [MaxLength(64)]
    public string RelatedEntityId { get; set; } = string.Empty;

    public int UploadedByUserId { get; set; }

    public User? UploadedByUser { get; set; }

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Description { get; set; }
}
