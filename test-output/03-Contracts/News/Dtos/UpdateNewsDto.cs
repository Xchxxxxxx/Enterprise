namespace MyApp.Contracts.News.Dtos;

public class UpdateNewsDto
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Author { get; set; }
    public bool IsPublished { get; set; }
}