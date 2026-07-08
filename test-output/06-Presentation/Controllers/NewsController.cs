using MyApp.Application.News.Services;
using MyApp.Contracts.News.Dtos;
using EfCore.Enterprise.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly NewsService _service;

    public NewsController(NewsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetPage([FromQuery] PagedRequest request)
    {
        var result = await _service.GetPageAsync(request);
        return Ok(ApiResult<PagedResult<NewsDto>>.Success(result));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(ApiResult<NewsDto>.Success(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNewsDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResult<NewsDto>.Success(result));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateNewsDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(ApiResult<NewsDto>.Success(result));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResult.Success());
    }
}