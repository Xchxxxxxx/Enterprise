using EfCore.Enterprise.Shared.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace EfCore.Enterprise.Infrastructure.Services;

public class GrayRule
{
    public string PathPrefix { get; set; } = string.Empty;
    public double Percentage { get; set; }
    public string? GrayVersion { get; set; }
    public List<string> WhiteListUsers { get; set; } = new();
}

[Injectable(ServiceLifetime.Singleton)]
public class GrayReleaseRuleManager
{
    private readonly ConcurrentDictionary<string, GrayRule> _rules = new();

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

    public void RemoveRule(string pathPrefix)
    {
        _rules.TryRemove(pathPrefix, out _);
    }

    public GrayRule? MatchRule(string path)
    {
        return _rules.Values.FirstOrDefault(r => path.StartsWith(r.PathPrefix));
    }
}