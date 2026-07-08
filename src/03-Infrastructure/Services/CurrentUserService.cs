using System.Security.Claims;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Shared.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace EfCore.Enterprise.Infrastructure.Services;

[Injectable(Lifetime = ServiceLifetime.Scoped)]
public class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public long? UserId
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)
                ?? User?.FindFirst("sub")
                ?? User?.FindFirst("userId");
            return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
        }
    }

    public string? UserName
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.Name)
                ?? User?.FindFirst("unique_name")
                ?? User?.FindFirst("username");
            return claim?.Value;
        }
    }

    public IEnumerable<long> Roles
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.Role)
                ?? User?.FindFirst("roles");
            if (claim == null) return Array.Empty<long>();
            return claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => long.TryParse(s, out var id) ? id : (long?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value);
        }
    }

    public IEnumerable<string> Permissions
    {
        get
        {
            var claim = User?.FindFirst("permissions")
                ?? User?.FindFirst("scope");
            if (claim == null) return Array.Empty<string>();
            return claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim());
        }
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public long? TenantId
    {
        get
        {
            var claim = User?.FindFirst("tenant_id")
                ?? User?.FindFirst("tenantId");
            return claim != null && long.TryParse(claim.Value, out var id) ? id : null;
        }
    }

    public string? ClientIp =>
        _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
}