namespace EfCore.Enterprise.Domain.Entities.Permission;

public class FieldPermissionConfig
{
    private FieldPermissionConfig() { }

    public FieldPermissionConfig(long permissionId, string entityName, string fieldName,
        bool isHidden = false, bool isMasked = false, string? maskRule = null)
    {
        PermissionId = permissionId;
        EntityName = entityName;
        FieldName = fieldName;
        IsHidden = isHidden;
        IsMasked = isMasked;
        MaskRule = maskRule;
    }

    public long PermissionId { get; private set; }
    public string EntityName { get; private set; } = string.Empty;
    public string FieldName { get; private set; } = string.Empty;
    public bool IsHidden { get; private set; }
    public bool IsMasked { get; private set; }
    public string? MaskRule { get; private set; }
}