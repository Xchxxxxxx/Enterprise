using MyApp.Application.Categories.Services;
using MyApp.Contracts.Categories.Dtos;
using EfCore.Enterprise.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _service;

    public CategoriesController(CategoryService service)
    {
        _service = service;
    }

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree(CancellationToken ct)
    {
        var result = await _service.GetTreeAsync(ct);
        return Ok(ApiResult<List<CategoryDto>>.Success(result));
    }

    [HttpGet("{parentId:long}/children")]
    public async Task<IActionResult> GetChildren(long parentId, CancellationToken ct)
    {
        var result = await _service.GetChildrenAsync(parentId, ct);
        return Ok(ApiResult<List<CategoryDto>>.Success(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto, CancellationToken ct)
    {
        var result = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResult<CategoryDto>.Success(result));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(ApiResult<CategoryDto>.Success(result));
    }

    [HttpPut("{id:long}/move")]
    public async Task<IActionResult> Move(long id, [FromQuery] long? parentId, CancellationToken ct)
    {
        await _service.MoveAsync(id, parentId, ct);
        return Ok(ApiResult.Success());
    }
}