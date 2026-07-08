namespace MyApp.Contracts.Categories.Dtos;

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public long? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Icon { get; set; }
}