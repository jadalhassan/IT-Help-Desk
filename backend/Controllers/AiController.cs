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
public class AiController(AppDbContext db, IAiService aiService) : ControllerBase
{
    private static readonly string[] Categories = ["Bug", "Feature Request", "Support", "Billing", "General"];

    [HttpGet("status")]
    public ActionResult<AiProviderStatusDto> GetStatus() => Ok(new AiProviderStatusDto(aiService.ProviderName, aiService.IsConfigured));

    [HttpPost("tickets/{id:int}/categorize")]
    public async Task<ActionResult<AiTicketCategoryDto>> CategorizeTicket(int id)
    {
        var ticket = await LoadTicketAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanView(ticket))
        {
            return Forbid();
        }

        try
        {
            return Ok(await aiService.CategorizeTicketAsync(ticket, Categories, HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("tickets/{id:int}/recommend-priority")]
    public async Task<ActionResult<AiTicketPriorityDto>> RecommendPriority(int id)
    {
        var ticket = await LoadTicketAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanView(ticket))
        {
            return Forbid();
        }

        try
        {
            return Ok(await aiService.RecommendPriorityAsync(ticket, HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("tickets/{id:int}/summarize")]
    public async Task<ActionResult<AiTicketSummaryDto>> SummarizeTicket(int id)
    {
        var ticket = await LoadTicketAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanView(ticket))
        {
            return Forbid();
        }

        try
        {
            return Ok(await aiService.SummarizeTicketAsync(ticket, HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("tickets/{id:int}/troubleshooting")]
    public async Task<ActionResult<AiTroubleshootingDto>> Troubleshooting(int id)
    {
        var ticket = await LoadTicketAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        if (!CanView(ticket))
        {
            return Forbid();
        }

        try
        {
            return Ok(await aiService.SuggestTroubleshootingAsync(ticket, HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("chat")]
    public async Task<ActionResult<AiChatResponseDto>> Chat(AiChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message is required." });
        }

        var focusedTicket = request.TicketId.HasValue ? await LoadTicketAsync(request.TicketId.Value) : null;
        if (request.TicketId.HasValue && focusedTicket is null)
        {
            return NotFound();
        }

        if (focusedTicket is not null && !CanView(focusedTicket))
        {
            return Forbid();
        }

        var visibleTickets = await VisibleTickets()
            .Include(ticket => ticket.CreatorUser)
            .Include(ticket => ticket.AssignedAgent)
            .Include(ticket => ticket.Comments)
            .OrderByDescending(ticket => ticket.UpdatedAtUtc)
            .Take(20)
            .ToListAsync();

        try
        {
            return Ok(await aiService.AnswerChatAsync(request.Message.Trim(), visibleTickets, focusedTicket, HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("knowledge-base/ask")]
    public Task<ActionResult<AiChatResponseDto>> AskKnowledgeBase(AiChatRequest request) => Chat(request);

    private async Task<Ticket?> LoadTicketAsync(int id) => await db.Tickets
        .Include(ticket => ticket.CreatorUser)
        .Include(ticket => ticket.AssignedAgent)
        .Include(ticket => ticket.Comments).ThenInclude(comment => comment.AuthorUser)
        .AsNoTracking()
        .FirstOrDefaultAsync(ticket => ticket.Id == id);

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

    private bool CanView(Ticket ticket)
    {
        var currentUserId = CurrentUserId();
        return CurrentRole() switch
        {
            "Admin" => true,
            "Agent" => ticket.AssignedAgentId == currentUserId,
            _ => ticket.CreatorUserId == currentUserId
        };
    }

    private int CurrentUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }

    private string CurrentRole() => User.FindFirstValue(ClaimTypes.Role) ?? "User";
}
