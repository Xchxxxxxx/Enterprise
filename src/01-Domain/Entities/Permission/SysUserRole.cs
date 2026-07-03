namespace EfCore.Enterprise.Domain.Entities.Permission;

public class SysUserRole : BaseAuditEntity
{
    private SysUserRole() { }

    public SysUserRole(long userId, long roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public long UserId { get; private set; }
    public long RoleId { get; private set; }
}