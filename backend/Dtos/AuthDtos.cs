namespace HelpDesk.Api.Dtos;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string FullName, string Email, string Role, DateTime ExpiresAtUtc);