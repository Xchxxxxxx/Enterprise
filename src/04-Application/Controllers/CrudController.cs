using EfCore.Enterprise.Application.Crud;
using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace EfCore.Enterprise.Application.Controllers;

public abstract class CrudController<TEntity, TDto, TCreateDto, TUpdateDto>
    : BaseApiController
    where TEntity : BaseEntity<long>
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    protected readonly ICrudAppService<TEntity, TDto, TCreateDto, TUpdateDto> _appService;

    protected CrudController(ICrudAppService<TEntity, TDto, TCreateDto, TUpdateDto> appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetPage([FromQuery] PagedRequest request)
    {
        var result = await _appService.GetPageAsync(request);
        return Success(result);
    }

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

    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] TCreateDto dto)
    {
        var result = await _appService.CreateAsync(dto);
        return Success(result);
    }

    [HttpPut("{id}")]
    public virtual async Task<IActionResult> Update(long id, [FromBody] TUpdateDto dto)
    {
        var result = await _appService.UpdateAsync(id, dto);
        return Success(result);
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(long id)
    {
        var result = await _appService.DeleteAsync(id);
        return Success(result);
    }
}