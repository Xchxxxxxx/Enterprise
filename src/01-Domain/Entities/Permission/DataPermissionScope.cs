namespace EfCore.Enterprise.Domain.Entities.Permission;

public class DataPermissionScope
{
    private DataPermissionScope() { }

    public DataPermissionScope(long roleId, string scopeType, string entityName, string filterExpression)
    {
        RoleId = roleId;
        ScopeType = scopeType;
        EntityName = entityName;
        FilterExpression = filterExpression;
    }

    public long RoleId { get; private set; }
    public string ScopeType { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string FilterExpression { get; private set; } = string.Empty;
}