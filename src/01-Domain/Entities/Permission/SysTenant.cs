namespace EfCore.Enterprise.Domain.Entities.Permission;

public class SysTenant : BaseFullEntity
{
    private SysTenant() { }

    public SysTenant(string code, string name, string? connectionString = null)
    {
        Code = code;
        Name = name;
        ConnectionString = connectionString;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ConnectionString { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    public void UpdateInfo(string name, string? connectionString)
    {
        Name = name;
        ConnectionString = connectionString;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}