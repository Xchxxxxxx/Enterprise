namespace EfCore.Enterprise.Domain.Entities.Permission;

public class SysRole : BaseFullEntity
{
    private SysRole() { }

    public SysRole(string code, string name, string? description = null, bool isSystem = false)
    {
        Code = code;
        Name = name;
        Description = description;
        IsSystem = isSystem;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; } = false;
    public bool IsEnabled { get; private set; } = true;

    public void UpdateInfo(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}