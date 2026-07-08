namespace MyApp.Contracts.News.Dtos;

public class NewsDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Author { get; set; }
    public DateTimeOffset PublishTime { get; set; }
    public bool IsPublished { get; set; }
}