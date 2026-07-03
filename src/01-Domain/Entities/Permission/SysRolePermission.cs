namespace EfCore.Enterprise.Domain.Entities.Permission;

public class SysRolePermission : BaseAuditEntity
{
    private SysRolePermission() { }

    public SysRolePermission(long roleId, long permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }

    public long RoleId { get; private set; }
    public long PermissionId { get; private set; }
}