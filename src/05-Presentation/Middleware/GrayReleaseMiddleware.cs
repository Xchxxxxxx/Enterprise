using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Presentation.Middleware;

/// <summary>
/// 灰度发布规则管理器，以单例方式存储所有灰度规则
/// </summary>
public class GrayReleaseRuleManager
{
    private readonly ConcurrentDictionary<string, GrayRule> _rules = new();

    /// <summary>
    /// 添加一条灰度发布规则
    /// </summary>
    /// <param name="pathPrefix">路径前缀</param>
    /// <param name="percentage">灰度比例（0~1）</param>
    /// <param name="grayVersion">灰度版本号</param>
    /// <param name="whiteListUsers">白名单用户ID列表</param>
    public void AddRule(string pathPrefix, double percentage, string? grayVersion = null, List<string>? whiteListUsers = null)
    {
        _rules[pathPrefix] = new GrayRule
        {
            PathPrefix = pathPrefix,
            Percentage = percentage,
            GrayVersion = grayVersion,
            WhiteListUsers = whiteListUsers ?? new List<string>()
        };
    }

    /// <summary>
    /// 移除一条灰度发布规则
    /// </summary>
    public void RemoveRule(string pathPrefix)
    {
        _rules.TryRemove(pathPrefix, out _);
    }

    /// <summary>
    /// 根据请求路径匹配灰度规则
    /// </summary>
    public GrayRule? MatchRule(string path)
    {
        return _rules.Values.FirstOrDefault(r => path.StartsWith(r.PathPrefix));
    }
}

/// <summary>
/// 灰度发布中间件，根据规则将部分流量路由到灰度版本
/// </summary>
public class GrayReleaseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GrayReleaseMiddleware> _logger;
    private readonly GrayReleaseRuleManager _ruleManager;

    /// <summary>
    /// 初始化灰度发布中间件
    /// </summary>
    public GrayReleaseMiddleware(RequestDelegate next, ILogger<GrayReleaseMiddleware> logger, GrayReleaseRuleManager ruleManager)
    {
        _next = next;
        _logger = logger;
        _ruleManager = ruleManager;
    }

    /// <summary>
    /// 执行灰度判断并设置请求头
    /// </summary>
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

    /// <summary>
    /// 判断用户是否命中灰度规则
    /// </summary>
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

/// <summary>
/// 灰度发布规则定义
/// </summary>
public class GrayRule
{
    /// <summary>路径前缀</summary>
    public string PathPrefix { get; set; } = string.Empty;

    /// <summary>灰度比例（0~1）</summary>
    public double Percentage { get; set; }

    /// <summary>灰度版本号</summary>
    public string? GrayVersion { get; set; }

    /// <summary>白名单用户ID列表</summary>
    public List<string> WhiteListUsers { get; set; } = new();
}