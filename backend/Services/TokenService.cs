using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HelpDesk.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace HelpDesk.Api.Services;

public class TokenService(IConfiguration configuration) : ITokenService
{
    public string CreateToken(User user, out DateTime expiresAtUtc)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("JWT issuer missing");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("JWT audience missing");
        var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("JWT secret missing");
        var expiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var parsed) ? parsed : 60;

        expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
