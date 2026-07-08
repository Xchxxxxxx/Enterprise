using EfCore.Enterprise.Application.CQRS.Queries;
using EfCore.Enterprise.Shared.Models;
using MyApp.Contracts.Products.Dtos;

namespace MyApp.Application.Products.Queries;

public class GetProductListQuery : BaseQuery<PagedResult<ProductDto>>
{
    public string? Keyword { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}