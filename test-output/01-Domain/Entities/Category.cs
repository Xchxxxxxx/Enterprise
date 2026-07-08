using EfCore.Enterprise.Domain.Entities;

namespace MyApp.Domain.Entities;

/// <summary>
/// 分类实体 - 演示树形结构（BaseTreeEntity）
/// </summary>
public class Category : BaseTreeEntity<Category>
{
    private Category() { }

    public Category(string name, long? parentId = null)
        : base(name, parentId)
    {
        Code = string.Empty;
    }

    public string Code { get; private set; }
    public string? Icon { get; private set; }

    public void SetCode(string code)
    {
        Code = code;
    }

    public void SetIcon(string? icon)
    {
        Icon = icon;
    }
}