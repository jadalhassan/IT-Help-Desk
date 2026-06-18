namespace HelpDesk.Api.Dtos;

public record ReportMetricDto(string Label, int Value);

public record ReportBreakdownDto(string Name, int Count);

public record TicketReportRowDto(
    int Id,
    string Title,
    string Category,
    string Priority,
    string Status,
    string CreatorName,
    string AssignedAgentName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ResolvedAtUtc,
    double? ResolutionHours);

public record TicketReportSummaryDto(
    int TotalTickets,
    int OpenTickets,
    int PendingTickets,
    int ResolvedTickets,
    int ClosedTickets,
    int OverdueTickets,
    double? AverageResolutionHours,
    IReadOnlyList<ReportBreakdownDto> ByStatus,
    IReadOnlyList<ReportBreakdownDto> ByPriority,
    IReadOnlyList<ReportBreakdownDto> ByCategory,
    IReadOnlyList<ReportBreakdownDto> ByAssignedAgent);

public record TicketReportFiltersDto(
    string? StartDate,
    string? EndDate,
    string? Status,
    string? Priority,
    string? Category,
    int? AssignedAgentId,
    int? CreatorUserId,
    string? Search);

public record TicketReportDto(
    TicketReportFiltersDto Filters,
    TicketReportSummaryDto Summary,
    IReadOnlyList<TicketReportRowDto> Tickets);

public record ReportFilterOptionsDto(
    IReadOnlyList<string> Statuses,
    IReadOnlyList<string> Priorities,
    IReadOnlyList<string> Categories,
    IReadOnlyList<UserSummaryDto> Agents,
    IReadOnlyList<UserSummaryDto> Creators);
