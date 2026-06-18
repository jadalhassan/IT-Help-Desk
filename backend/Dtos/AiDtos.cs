using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Dtos;

public record AiTicketCategoryDto(string Category, double Confidence, string Reason);

public record AiTicketPriorityDto(string Priority, double Confidence, string Reason);

public record AiTicketSummaryDto(string Summary);

public record AiTroubleshootingDto(IReadOnlyList<string> Suggestions);

public record AiChatResponseDto(string Answer);

public record AiProviderStatusDto(string Provider, bool Configured);

public class AiChatRequest
{
    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    public int? TicketId { get; set; }
}
