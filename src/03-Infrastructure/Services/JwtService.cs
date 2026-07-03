using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EfCore.Enterprise.Domain.Entities.Identity;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Infrastructure.Data;
using EfCore.Enterprise.Shared.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EfCore.Enterprise.Infrastructure.Services;

[Injectable(ServiceLifetime.Singleton)]
public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public JwtService(IConfiguration configuration, IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _secretKey = configuration["Jwt:SecretKey"]!;
        _issuer = configuration["Jwt:Issuer"]!;
        _audience = configuration["Jwt:Audience"]!;
        _accessTokenExpirationMinutes = configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 30);
        _refreshTokenExpirationDays = configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);

        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    }

    public (string accessToken, string refreshToken) GenerateTokens(long userId, string username, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var jwtId = Guid.NewGuid().ToString("N");
        var accessToken = GenerateAccessToken(userId, username, jwtId, roles, permissions);
        var refreshToken = GenerateRefreshToken(userId, jwtId);

        return (accessToken, refreshToken);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public string? GetJwtId(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            return jwtToken.Id;
        }
        catch
        {
            return null;
        }
    }

    public (string accessToken, string refreshToken) RefreshToken(string refreshToken, string accessToken)
    {
        var principal = ValidateToken(accessToken);
        if (principal == null)
        {
            throw new SecurityTokenException("Invalid access token");
        }

        var jwtId = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var userId = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var username = principal.FindFirstValue(ClaimTypes.Name)!;
        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
        var permissions = principal.FindAll("permission").Select(c => c.Value);

        var storedToken = GetStoredRefreshToken(refreshToken);
        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTimeOffset.UtcNow || storedToken.JwtId != jwtId)
        {
            throw new SecurityTokenException("Invalid refresh token");
        }

        RevokeStoredRefreshToken(storedToken, replacedByToken: null);

        return GenerateTokens(userId, username, roles, permissions);
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var storedToken = GetStoredRefreshToken(refreshToken);
        if (storedToken != null)
        {
            RevokeStoredRefreshToken(storedToken, replacedByToken: null);
        }
        await Task.CompletedTask;
    }

    public async Task RevokeAllUserTokensAsync(long userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokens = await db.Set<RefreshToken>()
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync();
        foreach (var token in tokens)
        {
            token.Revoke(null);
        }
        await db.SaveChangesAsync();
    }

    private string GenerateAccessToken(long userId, string username, string jwtId, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(_accessTokenExpirationMinutes).ToUnixTimeSeconds().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken(long userId, string jwtId)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Set<RefreshToken>().Add(new RefreshToken(userId, token, jwtId,
            DateTimeOffset.UtcNow.AddDays(_refreshTokenExpirationDays)));
        db.SaveChanges();

        return token;
    }

    private RefreshToken? GetStoredRefreshToken(string token)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return db.Set<RefreshToken>().FirstOrDefault(t => t.Token == token);
    }

    private void RevokeStoredRefreshToken(RefreshToken token, string? replacedByToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        token.Revoke(null, replacedByToken);
        db.Set<RefreshToken>().Update(token);
        db.SaveChanges();
    }
}