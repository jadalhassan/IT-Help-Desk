using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult PublicEndpoint() => Ok(new { message = "Public endpoint is reachable." });

    [HttpGet("user")]
    [Authorize]
    public IActionResult UserEndpoint() => Ok(new { message = "Authenticated users can view tickets." });

    [HttpGet("agent")]
    [Authorize(Policy = "AgentOrAdmin")]
    public IActionResult AgentEndpoint() => Ok(new { message = "Agents and Admins can manage tickets." });

    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult AdminEndpoint() => Ok(new { message = "Admins only endpoint." });
}