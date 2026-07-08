namespace EfCore.Enterprise.Domain.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class OpLogAttribute : Attribute
{
    public string Module { get; }
    public string Action { get; }
    public string? DetailTemplate { get; }

    public OpLogAttribute(string module, string action, string? detailTemplate = null)
    {
        Module = module;
        Action = action;
        DetailTemplate = detailTemplate;
    }
}