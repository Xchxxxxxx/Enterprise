namespace EfCore.Enterprise.Domain.Entities.Permission;

public class SysPermission : BaseFullEntity
{
    private SysPermission() { }

    public SysPermission(string code, string name, string? description = null, string? groupName = null)
    {
        Code = code;
        Name = name;
        Description = description;
        GroupName = groupName;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? GroupName { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    public void UpdateInfo(string name, string? description, string? groupName)
    {
        Name = name;
        Description = description;
        GroupName = groupName;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}