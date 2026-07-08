using EfCore.Enterprise.Domain.Entities;

namespace MyApp.Domain.Entities;

/// <summary>
/// 新闻实体 - 演示 BaseEntity&lt;long&gt; + CrudAppService 泛型基类
/// 使用雪花ID生成器自动分配主键
/// </summary>
public class News : BaseEntity<long>
{
    private News() { }

    public News(string title, string content)
    {
        Title = title;
        Content = content;
        PublishTime = DateTimeOffset.UtcNow;
    }

    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public string? Author { get; private set; }
    public DateTimeOffset PublishTime { get; private set; }
    public bool IsPublished { get; private set; }

    public void Publish()
    {
        IsPublished = true;
        PublishTime = DateTimeOffset.UtcNow;
    }

    public void Unpublish()
    {
        IsPublished = false;
    }

    public void SetSummary(string? summary)
    {
        Summary = summary;
    }
}