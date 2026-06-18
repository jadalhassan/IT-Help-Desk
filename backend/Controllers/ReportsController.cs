using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HelpDesk.Api.Data;
using HelpDesk.Api.Dtos;
using HelpDesk.Api.Models;
using HelpDesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController(AppDbContext db, IReportExportService exportService) : ControllerBase
{
    [HttpGet("tickets")]
    public async Task<ActionResult<TicketReportDto>> GetTicketReport(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] string? category,
        [FromQuery] int? assignedAgentId,
        [FromQuery] int? creatorUserId,
        [FromQuery] string? search)
    {
        var validation = ValidateDates(startDate, endDate, out var start, out var end);
        if (validation is not null)
        {
            return validation;
        }

        var report = await BuildTicketReportAsync(new TicketReportFiltersDto(
            startDate,
            endDate,
            Clean(status),
            Clean(priority),
            Clean(category),
            assignedAgentId,
            creatorUserId,
            Clean(search)),
            start,
            end);

        return Ok(report);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<TicketReportSummaryDto>> GetSummary()
    {
        var report = await BuildTicketReportAsync(new TicketReportFiltersDto(null, null, null, null, null, null, null, null), null, null);
        return Ok(report.Summary);
    }

    [HttpGet("filters")]
    public async Task<ActionResult<ReportFilterOptionsDto>> GetFilters()
    {
        var visible = VisibleTickets();
        var agents = await db.Users.AsNoTracking()
            .Where(user => user.Role == "Agent" && visible.Any(ticket => ticket.AssignedAgentId == user.Id))
            .OrderBy(user => user.FullName)
            .Select(user => new UserSummaryDto(user.Id, user.FullName, user.Email, user.Role))
            .ToListAsync();
        var creators = await db.Users.AsNoTracking()
            .Where(user => visible.Any(ticket => ticket.CreatorUserId == user.Id))
            .OrderBy(user => user.FullName)
            .Select(user => new UserSummaryDto(user.Id, user.FullName, user.Email, user.Role))
            .ToListAsync();

        return Ok(new ReportFilterOptionsDto(
            await visible.Select(ticket => ticket.Status).Distinct().OrderBy(value => value).ToListAsync(),
            await visible.Select(ticket => ticket.Priority).Distinct().OrderBy(value => value).ToListAsync(),
            await visible.Select(ticket => ticket.Category).Distinct().OrderBy(value => value).ToListAsync(),
            agents,
            creators));
    }

    [HttpGet("tickets/export/pdf")]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] string? category,
        [FromQuery] int? assignedAgentId,
        [FromQuery] int? creatorUserId,
        [FromQuery] string? search)
    {
        var validation = ValidateDates(startDate, endDate, out var start, out var end);
        if (validation is not null)
        {
            return validation;
        }

        var report = await BuildTicketReportAsync(new TicketReportFiltersDto(startDate, endDate, Clean(status), Clean(priority), Clean(category), assignedAgentId, creatorUserId, Clean(search)), start, end);
        var bytes = exportService.CreatePdf(report, DateTime.UtcNow);
        return File(bytes, "application/pdf", $"tickets-report-{DateTime.UtcNow:yyyy-MM-dd}.pdf");
    }

    [HttpGet("tickets/export/excel")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] string? category,
        [FromQuery] int? assignedAgentId,
        [FromQuery] int? creatorUserId,
        [FromQuery] string? search)
    {
        var validation = ValidateDates(startDate, endDate, out var start, out var end);
        if (validation is not null)
        {
            return validation;
        }

        var report = await BuildTicketReportAsync(new TicketReportFiltersDto(startDate, endDate, Clean(status), Clean(priority), Clean(category), assignedAgentId, creatorUserId, Clean(search)), start, end);
        var bytes = exportService.CreateExcel(report, DateTime.UtcNow);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"tickets-report-{DateTime.UtcNow:yyyy-MM-dd}.xlsx");
    }

    private async Task<TicketReportDto> BuildTicketReportAsync(TicketReportFiltersDto filters, DateTime? start, DateTime? end)
    {
        var query = VisibleTickets()
            .Include(ticket => ticket.CreatorUser)
            .Include(ticket => ticket.AssignedAgent)
            .Include(ticket => ticket.StatusHistory)
            .AsQueryable();

        if (start.HasValue)
        {
            query = query.Where(ticket => ticket.CreatedAtUtc >= start.Value);
        }

        if (end.HasValue)
        {
            query = query.Where(ticket => ticket.CreatedAtUtc < end.Value.AddDays(1));
        }

        if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            query = query.Where(ticket => ticket.Status == filters.Status);
        }

        if (!string.IsNullOrWhiteSpace(filters.Priority))
        {
            query = query.Where(ticket => ticket.Priority == filters.Priority);
        }

        if (!string.IsNullOrWhiteSpace(filters.Category))
        {
            query = query.Where(ticket => ticket.Category == filters.Category);
        }

        if (filters.AssignedAgentId.HasValue)
        {
            query = query.Where(ticket => ticket.AssignedAgentId == filters.AssignedAgentId);
        }

        if (filters.CreatorUserId.HasValue)
        {
            query = query.Where(ticket => ticket.CreatorUserId == filters.CreatorUserId);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            query = query.Where(ticket => ticket.Title.Contains(filters.Search) || ticket.Description.Contains(filters.Search));
        }

        var tickets = await query.OrderByDescending(ticket => ticket.CreatedAtUtc).ToListAsync();
        var overdueCutoff = DateTime.UtcNow.AddDays(-3);
        var resolvedHours = tickets
            .Select(ResolutionHours)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToList();

        var summary = new TicketReportSummaryDto(
            tickets.Count,
            tickets.Count(ticket => ticket.Status == "Open"),
            tickets.Count(ticket => ticket.Status is not "Resolved" and not "Closed"),
            tickets.Count(ticket => ticket.Status == "Resolved"),
            tickets.Count(ticket => ticket.Status == "Closed"),
            tickets.Count(ticket => ticket.Status is not "Resolved" and not "Closed" && ticket.CreatedAtUtc < overdueCutoff),
            resolvedHours.Count > 0 ? Math.Round(resolvedHours.Average(), 2) : null,
            Breakdown(tickets, ticket => ticket.Status),
            Breakdown(tickets, ticket => ticket.Priority),
            Breakdown(tickets, ticket => ticket.Category),
            Breakdown(tickets, ticket => ticket.AssignedAgent?.FullName ?? "Unassigned"));

        return new TicketReportDto(filters, summary, tickets.Select(ToRow).ToList());
    }

    private IQueryable<Ticket> VisibleTickets()
    {
        var currentUserId = CurrentUserId();
        return CurrentRole() switch
        {
            "Admin" => db.Tickets.AsNoTracking(),
            "Agent" => db.Tickets.AsNoTracking().Where(ticket => ticket.AssignedAgentId == currentUserId),
            _ => db.Tickets.AsNoTracking().Where(ticket => ticket.CreatorUserId == currentUserId)
        };
    }

    private static TicketReportRowDto ToRow(Ticket ticket) => new(
        ticket.Id,
        ticket.Title,
        ticket.Category,
        ticket.Priority,
        ticket.Status,
        ticket.CreatorUser?.FullName ?? "Unknown",
        ticket.AssignedAgent?.FullName ?? "Unassigned",
        ticket.CreatedAtUtc,
        ticket.UpdatedAtUtc,
        ResolvedAt(ticket),
        ResolutionHours(ticket));

    private static DateTime? ResolvedAt(Ticket ticket) =>
        ticket.StatusHistory
            .Where(history => history.NewStatus is "Resolved" or "Closed")
            .OrderBy(history => history.ChangedAtUtc)
            .Select(history => (DateTime?)history.ChangedAtUtc)
            .FirstOrDefault();

    private static double? ResolutionHours(Ticket ticket)
    {
        var resolvedAt = ResolvedAt(ticket);
        return resolvedAt.HasValue ? (resolvedAt.Value - ticket.CreatedAtUtc).TotalHours : null;
    }

    private static IReadOnlyList<ReportBreakdownDto> Breakdown(IEnumerable<Ticket> tickets, Func<Ticket, string> selector) =>
        tickets.GroupBy(selector)
            .Select(group => new ReportBreakdownDto(group.Key, group.Count()))
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Name)
            .ToList();

    private BadRequestObjectResult? ValidateDates(string? startDate, string? endDate, out DateTime? start, out DateTime? end)
    {
        start = null;
        end = null;

        if (!string.IsNullOrWhiteSpace(startDate) && !DateTime.TryParse(startDate, out var parsedStart))
        {
            return BadRequest(new { message = "Invalid start date." });
        }

        if (!string.IsNullOrWhiteSpace(endDate) && !DateTime.TryParse(endDate, out var parsedEnd))
        {
            return BadRequest(new { message = "Invalid end date." });
        }

        start = string.IsNullOrWhiteSpace(startDate) ? null : DateTime.SpecifyKind(DateTime.Parse(startDate).Date, DateTimeKind.Utc);
        end = string.IsNullOrWhiteSpace(endDate) ? null : DateTime.SpecifyKind(DateTime.Parse(endDate).Date, DateTimeKind.Utc);

        if (start.HasValue && end.HasValue && start > end)
        {
            return BadRequest(new { message = "Start date must be before end date." });
        }

        return null;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) || value == "All" ? null : value.Trim();

    private int CurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }

    private string CurrentRole() => User.FindFirstValue(ClaimTypes.Role) ?? "User";
}
