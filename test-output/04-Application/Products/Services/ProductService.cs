using System.Linq.Expressions;
using AutoMapper;
using EfCore.Enterprise.Application.Services;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Shared.Models;
using MyApp.Application.Products.Queries;
using MyApp.Contracts.Products.Dtos;
using MyApp.Domain.Entities;

namespace MyApp.Application.Products.Services;

public class ProductService : BaseService
{
    private readonly ISuperRepository<Product> _repository;

    public ProductService(
        ISuperRepository<Product> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(unitOfWork, mapper)
    {
        _repository = repository;
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        var product = new Product(dto.Name, dto.Price, dto.Stock);
        if (!string.IsNullOrWhiteSpace(dto.Description))
            product.ChangeDescription(dto.Description);

        await _repository.AddAsync(product, ct);
        await UnitOfWork.SaveChangesAsync(ct);

        return Mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> UpdatePriceAsync(UpdateProductPriceDto dto, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(dto.Id, ct);
        if (product is null)
            throw new InvalidOperationException($"产品 {dto.Id} 不存在");

        product.ChangePrice(dto.NewPrice);
        await UnitOfWork.SaveChangesAsync(ct);

        return Mapper.Map<ProductDto>(product);
    }

    public async Task<PagedResult<ProductDto>> GetPagedListAsync(GetProductListQuery query, CancellationToken ct = default)
    {
        Expression<Func<Product, bool>> predicate = x =>
            (string.IsNullOrWhiteSpace(query.Keyword) || x.Name.Contains(query.Keyword)) &&
            (!query.MinPrice.HasValue || x.Price >= query.MinPrice.Value) &&
            (!query.MaxPrice.HasValue || x.Price <= query.MaxPrice.Value);

        var request = new PagedRequest
        {
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };

        var result = await _repository.GetPagedAsync(predicate, request, ct);

        return new PagedResult<ProductDto>(
            Mapper.Map<List<ProductDto>>(result.Items),
            result.TotalCount,
            result.PageIndex,
            result.PageSize);
    }

    public async Task<ProductDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var product = await _repository.GetByIdAsync(id, ct);
        return product is null ? null : Mapper.Map<ProductDto>(product);
    }
}