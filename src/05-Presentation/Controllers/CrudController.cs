using EfCore.Enterprise.Application.Crud;
using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace EfCore.Enterprise.Presentation.Controllers;

/// <summary>
/// 泛型CRUD控制器基类，自动生成标准CRUD端点（GET/POST/PUT/DELETE）
/// </summary>
/// <remarks>
/// 继承此控制器即可自动获得完整的CRUD API，无需编写任何重复代码。
/// 所有方法均为 <c>virtual</c>，子类可按需重写。
/// <code>
/// [Route("api/orders")]
/// public class OrderController : CrudController&lt;Order, OrderDto, CreateOrderDto, UpdateOrderDto&gt;
/// {
///     public OrderController(IOrderService svc) : base(svc) { }
/// }
/// </code>
/// </remarks>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TDto">展示DTO类型</typeparam>
/// <typeparam name="TCreateDto">创建DTO类型</typeparam>
/// <typeparam name="TUpdateDto">更新DTO类型</typeparam>
public abstract class CrudController<TEntity, TDto, TCreateDto, TUpdateDto>
    : BaseApiController
    where TEntity : BaseEntity<long>
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    /// <summary>CRUD服务实例</summary>
    protected readonly ICrudAppService<TEntity, TDto, TCreateDto, TUpdateDto> _appService;

    /// <summary>
    /// 初始化CRUD控制器
    /// </summary>
    /// <param name="appService">对应的CRUD服务</param>
    protected CrudController(ICrudAppService<TEntity, TDto, TCreateDto, TUpdateDto> appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// GET /api/xxx — 分页查询
    /// </summary>
    [HttpGet]
    public virtual async Task<IActionResult> GetPage([FromQuery] PagedRequest request)
    {
        var result = await _appService.GetPageAsync(request);
        return Success(result);
    }

    /// <summary>
    /// GET /api/xxx/{id} — 根据ID查询单条记录
    /// </summary>
    [HttpGet("{id}")]
    public virtual async Task<IActionResult> GetById(long id)
    {
        var result = await _appService.GetByIdAsync(id);
        if (result == null)
        {
            return Fail("数据不存在", 404);
        }
        return Success(result);
    }

    /// <summary>
    /// POST /api/xxx — 创建新记录
    /// </summary>
    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] TCreateDto dto)
    {
        var result = await _appService.CreateAsync(dto);
        return Success(result);
    }

    /// <summary>
    /// PUT /api/xxx/{id} — 更新记录
    /// </summary>
    [HttpPut("{id}")]
    public virtual async Task<IActionResult> Update(long id, [FromBody] TUpdateDto dto)
    {
        var result = await _appService.UpdateAsync(id, dto);
        return Success(result);
    }

    /// <summary>
    /// DELETE /api/xxx/{id} — 删除记录（软删除）
    /// </summary>
    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(long id)
    {
        var result = await _appService.DeleteAsync(id);
        return Success(result);
    }
}