using HelpDesk.Api.Data;
using HelpDesk.Api.Dtos;
using HelpDesk.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class TicketsController(AppDbContext db) : ControllerBase
{
    private static readonly string[] Categories = ["Bug", "Feature Request", "Support", "Billing", "General"];
    private static readonly string[] Priorities = ["Low", "Medium", "High", "Urgent"];
    private static readonly string[] Statuses = ["Open", "In Progress", "Resolved", "Closed"];

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetTickets([FromQuery] string? category)
    {
        var tickets = db.Tickets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            tickets = tickets.Where(ticket => ticket.Category == category);
        }

        var result = await tickets
            .OrderByDescending(ticket => ticket.CreatedAtUtc)
            .Select(ticket => ToDto(ticket))
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDto>> GetTicket(int id)
    {
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(ticket => ticket.Id == id);

        return ticket is null ? NotFound() : Ok(ToDto(ticket));
    }

    [HttpPost]
    public async Task<ActionResult<TicketDto>> CreateTicket(TicketRequest request)
    {
        var validationError = ValidateTicketRequest(request);
        if (validationError is not null)
        {
            return validationError;
        }

        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category,
            Priority = request.Priority,
            Status = request.Status,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var dto = ToDto(ticket);
        return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TicketDto>> UpdateTicket(int id, TicketRequest request)
    {
        var validationError = ValidateTicketRequest(request);
        if (validationError is not null)
        {
            return validationError;
        }

        var ticket = await db.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        ticket.Title = request.Title.Trim();
        ticket.Description = request.Description.Trim();
        ticket.Category = request.Category;
        ticket.Priority = request.Priority;
        ticket.Status = request.Status;
        ticket.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(ToDto(ticket));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTicket(int id)
    {
        var ticket = await db.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        db.Tickets.Remove(ticket);
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("/api/categories")]
    public ActionResult<IEnumerable<string>> GetCategories() => Ok(Categories);

    private static BadRequestObjectResult? ValidateTicketRequest(TicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
        {
            return new BadRequestObjectResult(new { message = "Title and description are required." });
        }

        if (!Categories.Contains(request.Category))
        {
            return new BadRequestObjectResult(new { message = "Invalid ticket category." });
        }

        if (!Priorities.Contains(request.Priority))
        {
            return new BadRequestObjectResult(new { message = "Invalid ticket priority." });
        }

        if (!Statuses.Contains(request.Status))
        {
            return new BadRequestObjectResult(new { message = "Invalid ticket status." });
        }

        return null;
    }

    private static TicketDto ToDto(Ticket ticket) => new(
        ticket.Id,
        ticket.Title,
        ticket.Description,
        ticket.Category,
        ticket.Priority,
        ticket.Status,
        ticket.CreatedAtUtc,
        ticket.UpdatedAtUtc);
}
