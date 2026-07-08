using AutoMapper;
using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Shared.Exceptions;
using EfCore.Enterprise.Shared.Models;
using System.Linq.Expressions;

namespace EfCore.Enterprise.Application.Crud;

/// <summary>
/// 泛型CRUD服务基类，提供标准的分页、增删改查默认实现
/// </summary>
/// <remarks>
/// 继承此类即可获得完整的CRUD功能，无需编写任何重复代码。
/// 复杂业务场景可重写 <c>virtual</c> 方法，或重写 <see cref="ApplyFilter"/> 实现自定义过滤。
/// <code>
/// public class OrderService : CrudAppService&lt;Order, OrderDto, CreateOrderDto, UpdateOrderDto&gt;
/// {
///     public OrderService(ISuperRepository&lt;Order&gt; repo, IMapper mapper) : base(repo, mapper) { }
/// }
/// </code>
/// </remarks>
/// <typeparam name="TEntity">实体类型，必须继承BaseEntity</typeparam>
/// <typeparam name="TDto">展示DTO类型</typeparam>
/// <typeparam name="TCreateDto">创建DTO类型</typeparam>
/// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
public class CrudAppService<TEntity, TDto, TCreateDto, TUpdateDto>
    : ICrudAppService<TEntity, TDto, TCreateDto, TUpdateDto>
    where TEntity : BaseEntity<long>
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    protected readonly ISuperRepository<TEntity> _repository;
    protected readonly IMapper _mapper;
    protected readonly ICurrentUser _currentUser;

    public CrudAppService(ISuperRepository<TEntity> repository, IMapper mapper, ICurrentUser currentUser)
    {
        _repository = repository;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 分页查询，可重写 <see cref="ApplyFilter"/> 实现自定义过滤
    /// </summary>
    /// <param name="request">分页请求参数</param>
    /// <returns>分页结果</returns>
    public virtual async Task<PagedResult<TDto>> GetPageAsync(PagedRequest request)
    {
        var query = _repository.Query();
        query = ApplyFilter(query, request);
        var result = await _repository.GetPagedAsync(e => true, request);
        return _mapper.Map<PagedResult<TDto>>(result);
    }

    /// <summary>
    /// 根据主键ID获取单条记录
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns>DTO或null</returns>
    public virtual async Task<TDto?> GetByIdAsync(long id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return _mapper.Map<TDto?>(entity);
    }

    /// <summary>
    /// 创建新记录，自动映射DTO到实体
    /// </summary>
    /// <param name="dto">创建DTO</param>
    /// <returns>创建后的DTO</returns>
    public virtual async Task<TDto> CreateAsync(TCreateDto dto)
    {
        var entity = _mapper.Map<TEntity>(dto);
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<TDto>(entity);
    }

    /// <summary>
    /// 更新记录，先查后改，实体不存在时抛出 <see cref="NotFoundException"/>
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <param name="dto">更新DTO</param>
    /// <returns>更新后的DTO</returns>
    public virtual async Task<TDto> UpdateAsync(long id, TUpdateDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new NotFoundException();

        _mapper.Map(dto, entity);
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<TDto>(entity);
    }

    /// <summary>
    /// 删除记录（软删除），实体不存在时抛出 <see cref="NotFoundException"/>
    /// </summary>
    /// <param name="id">主键ID</param>
    /// <returns>始终返回true</returns>
    public virtual async Task<bool> DeleteAsync(long id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) throw new NotFoundException();

        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 自定义过滤条件，子类可重写以实现租户过滤、权限过滤等逻辑
    /// </summary>
    /// <param name="query">原始查询</param>
    /// <param name="request">分页请求</param>
    /// <returns>过滤后的查询</returns>
    protected virtual IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, PagedRequest request)
    {
        return query;
    }
}