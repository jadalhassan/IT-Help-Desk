using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Dtos;

public record TicketDto(
    int Id,
    string Title,
    string Description,
    string Category,
    string Priority,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

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
