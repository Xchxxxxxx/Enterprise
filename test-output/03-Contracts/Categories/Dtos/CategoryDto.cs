namespace MyApp.Contracts.Categories.Dtos;

public class CategoryDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string Path { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Sort { get; set; }
    public bool IsLeaf { get; set; }
}