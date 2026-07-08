using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace EfCore.Enterprise.Domain.Interfaces;

public interface IJwtService
{
    TokenValidationParameters GetValidationParameters();
    string GenerateAccessToken(long userId, string username, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    string? GetJwtId(string token);
}