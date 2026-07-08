using EfCore.Enterprise.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Middleware;

public class GrayReleaseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GrayReleaseMiddleware> _logger;
    private readonly GrayReleaseRuleManager _ruleManager;

    public GrayReleaseMiddleware(RequestDelegate next, ILogger<GrayReleaseMiddleware> logger, GrayReleaseRuleManager ruleManager)
    {
        _next = next;
        _logger = logger;
        _ruleManager = ruleManager;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var rule = _ruleManager.MatchRule(path);

        if (rule != null)
        {
            var userId = context.Request.Headers["X-User-Id"].FirstOrDefault();
            var isGray = IsGrayUser(rule, userId);

            context.Request.Headers["X-Gray-Release"] = isGray ? "true" : "false";
            _logger.LogDebug("灰度发布: 路径 {Path}, 用户 {UserId}, 灰度 {IsGray}", path, userId, isGray);

            if (isGray && !string.IsNullOrEmpty(rule.GrayVersion))
            {
                context.Request.Headers["X-Api-Version"] = rule.GrayVersion;
            }
        }

        await _next(context);
    }

    private static bool IsGrayUser(GrayRule rule, string? userId)
    {
        if (!string.IsNullOrEmpty(userId) && rule.WhiteListUsers.Contains(userId))
        {
            return true;
        }

        var hash = Math.Abs(userId?.GetHashCode() ?? Guid.NewGuid().GetHashCode());
        return hash % 100 < rule.Percentage * 100;
    }
}