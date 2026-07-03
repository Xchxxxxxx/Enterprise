namespace EfCore.Enterprise.Shared.Attributes;

/// <summary>
/// 标记一个实体需要自动生成CRUD端点，配合 <c>CrudController</c> 使用
/// </summary>
/// <remarks>
/// 可以精细控制哪些操作对外暴露。所有属性默认开启。
/// <code>
/// [AutoCrud(EnableDelete = false)]
/// public class Order : BaseEntity { }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AutoCrudAttribute : Attribute
{
    /// <summary>是否启用创建端点</summary>
    public bool EnableCreate { get; set; } = true;

    /// <summary>是否启用更新端点</summary>
    public bool EnableUpdate { get; set; } = true;

    /// <summary>是否启用删除端点</summary>
    public bool EnableDelete { get; set; } = true;

    /// <summary>是否启用单条查询端点</summary>
    public bool EnableRead { get; set; } = true;

    /// <summary>是否启用分页查询端点</summary>
    public bool EnablePage { get; set; } = true;
}