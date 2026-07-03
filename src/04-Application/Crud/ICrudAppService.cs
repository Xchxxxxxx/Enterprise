using EfCore.Enterprise.Shared.Models;

namespace EfCore.Enterprise.Application.Crud;

/// <summary>
/// 泛型CRUD服务接口，定义标准的分页、增删改查操作契约
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TDto">展示DTO类型</typeparam>
/// <typeparam name="TCreateDto">创建DTO类型</typeparam>
/// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
public interface ICrudAppService<TEntity, TDto, TCreateDto, TUpdateDto>
    where TEntity : class
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    /// <summary>
    /// 分页查询
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <returns>分页结果</returns>
    Task<PagedResult<TDto>> GetPageAsync(PagedRequest request);

    /// <summary>
    /// 根据主键ID获取单条记录
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns>DTO或null</returns>
    Task<TDto?> GetByIdAsync(long id);

    /// <summary>
    /// 创建新记录
    /// </summary>
    /// <param name="dto">创建DTO</param>
    /// <returns>创建后的DTO</returns>
    Task<TDto> CreateAsync(TCreateDto dto);

    /// <summary>
    /// 更新记录
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <param name="dto">更新DTO</param>
    /// <returns>更新后的DTO</returns>
    Task<TDto> UpdateAsync(long id, TUpdateDto dto);

    /// <summary>
    /// 删除记录（软删除）
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteAsync(long id);
}