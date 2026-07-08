using MyApp.Application.Products.Queries;
using MyApp.Application.Products.Services;
using MyApp.Contracts.Products.Dtos;
using EfCore.Enterprise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly IMediator _mediator;

    public ProductsController(ProductService productService, IMediator mediator)
    {
        _productService = productService;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GetProductListQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(ApiResult<PagedResult<ProductDto>>.Success(result));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _productService.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(ApiResult<ProductDto?>.Success(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var result = await _productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResult<ProductDto>.Success(result));
    }

    [HttpPut("{id:long}/price")]
    public async Task<IActionResult> UpdatePrice(long id, [FromBody] UpdateProductPriceDto dto)
    {
        dto.Id = id;
        var result = await _productService.UpdatePriceAsync(dto);
        return Ok(ApiResult<ProductDto>.Success(result));
    }
}