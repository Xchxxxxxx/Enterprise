namespace EfCore.Enterprise.Domain.Entities.Identity;

public class RefreshToken : BaseEntity
{
    private RefreshToken() { }

    public RefreshToken(long userId, string token, string jwtId, DateTimeOffset expiresAt)
    {
        UserId = userId;
        Token = token;
        JwtId = jwtId;
        ExpiresAt = expiresAt;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public long UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string JwtId { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public bool IsRevoked { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByToken { get; private set; }

    public void Revoke(string? revokedByIp, string? replacedByToken = null)
    {
        IsRevoked = true;
        RevokedAt = DateTimeOffset.UtcNow;
        RevokedByIp = revokedByIp;
        ReplacedByToken = replacedByToken;
    }
}