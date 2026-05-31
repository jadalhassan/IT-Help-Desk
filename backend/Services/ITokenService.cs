using HelpDesk.Api.Models;

namespace HelpDesk.Api.Services;

public interface ITokenService
{
    string CreateToken(User user, out DateTime expiresAtUtc);
}