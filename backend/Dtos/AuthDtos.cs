using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Dtos;

public class LoginRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}

public record LoginResponse(string Token, string FullName, string Email, string Role, DateTime ExpiresAtUtc);
