using HelpDesk.Api.Dtos;
using HelpDesk.Api.Models;

namespace HelpDesk.Api.Services;

public interface IAiService
{
    string ProviderName { get; }
    bool IsConfigured { get; }
    Task<AiTicketCategoryDto> CategorizeTicketAsync(Ticket ticket, IReadOnlyList<string> categories, CancellationToken cancellationToken);
    Task<AiTicketPriorityDto> RecommendPriorityAsync(Ticket ticket, CancellationToken cancellationToken);
    Task<AiTicketSummaryDto> SummarizeTicketAsync(Ticket ticket, CancellationToken cancellationToken);
    Task<AiTroubleshootingDto> SuggestTroubleshootingAsync(Ticket ticket, CancellationToken cancellationToken);
    Task<AiChatResponseDto> AnswerChatAsync(string message, IReadOnlyList<Ticket> tickets, Ticket? focusedTicket, CancellationToken cancellationToken);
}
