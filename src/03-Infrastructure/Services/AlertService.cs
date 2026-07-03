using EfCore.Enterprise.Shared.DependencyInjection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface IAlertService
{
    Task SendAlertAsync(string title, string message, AlertLevel level = AlertLevel.Warning);
    Task SendSlowSqlAlertAsync(string sql, long elapsedMs);
    Task SendErrorAlertAsync(string endpoint, Exception ex);
    Task SendServiceDownAlertAsync(string serviceName);
}

public enum AlertLevel
{
    Info,
    Warning,
    Error,
    Critical
}

[Injectable(ServiceLifetime.Singleton)]
public class AlertService : IAlertService
{
    private readonly ILogger<AlertService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public AlertService(ILogger<AlertService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendAlertAsync(string title, string message, AlertLevel level = AlertLevel.Warning)
    {
        _logger.Log(level switch
        {
            AlertLevel.Info => LogLevel.Information,
            AlertLevel.Warning => LogLevel.Warning,
            AlertLevel.Error => LogLevel.Error,
            AlertLevel.Critical => LogLevel.Critical,
            _ => LogLevel.Warning
        }, "告警: {Title} - {Message}", title, message);

        var webhookUrl = Environment.GetEnvironmentVariable("ALERT_WEBHOOK_URL");
        if (!string.IsNullOrEmpty(webhookUrl))
        {
            var payload = new
            {
                msgtype = "markdown",
                markdown = new
                {
                    title = title,
                    text = $"## {title}\n\n{message}\n\n> 级别: {level}\n> 时间: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}"
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            await _httpClientFactory.CreateClient().PostAsync(webhookUrl, content);
        }
    }

    public async Task SendSlowSqlAlertAsync(string sql, long elapsedMs)
    {
        await SendAlertAsync(
            "慢SQL告警",
            $"SQL执行耗时: {elapsedMs}ms\n\n```sql\n{sql}\n```",
            AlertLevel.Warning);
    }

    public async Task SendErrorAlertAsync(string endpoint, Exception ex)
    {
        await SendAlertAsync(
            "接口异常告警",
            $"接口: {endpoint}\n\n异常: {ex.Message}\n\n堆栈: {ex.StackTrace}",
            AlertLevel.Error);
    }

    public async Task SendServiceDownAlertAsync(string serviceName)
    {
        await SendAlertAsync(
            "服务宕机告警",
            $"服务 {serviceName} 不可用",
            AlertLevel.Critical);
    }
}