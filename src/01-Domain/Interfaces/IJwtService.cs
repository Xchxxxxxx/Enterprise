using System.Security.Claims;

namespace EfCore.Enterprise.Domain.Interfaces;

public interface IJwtService
{
    (string accessToken, string refreshToken) GenerateTokens(long userId, string username, IEnumerable<string> roles, IEnumerable<string> permissions);
    ClaimsPrincipal? ValidateToken(string token);
    string? GetJwtId(string token);
    (string accessToken, string refreshToken) RefreshToken(string refreshToken, string accessToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task RevokeAllUserTokensAsync(long userId);
}