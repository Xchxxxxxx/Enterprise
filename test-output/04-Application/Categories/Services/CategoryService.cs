using AutoMapper;
using EfCore.Enterprise.Application.Services;
using EfCore.Enterprise.Domain.Interfaces;
using MyApp.Contracts.Categories.Dtos;
using MyApp.Domain.Entities;

namespace MyApp.Application.Categories.Services;

/// <summary>
/// 分类服务 - 演示 ITreeRepository 树形操作
/// </summary>
public class CategoryService : BaseService
{
    private readonly ITreeRepository<Category> _repository;

    public CategoryService(
        ITreeRepository<Category> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(unitOfWork, mapper)
    {
        _repository = repository;
    }

    public async Task<List<CategoryDto>> GetTreeAsync(CancellationToken ct = default)
    {
        var categories = await _repository.GetTreeAsync(ct);
        return Mapper.Map<List<CategoryDto>>(categories);
    }

    public async Task<List<CategoryDto>> GetChildrenAsync(long parentId, CancellationToken ct = default)
    {
        var children = await _repository.GetChildrenAsync(parentId, ct);
        return Mapper.Map<List<CategoryDto>>(children);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default)
    {
        var category = new Category(dto.Name, dto.ParentId);
        category.SetCode(dto.Code);
        category.SetIcon(dto.Icon);

        await _repository.AddAsync(category, ct);
        await UnitOfWork.SaveChangesAsync(ct);

        return Mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var category = await _repository.GetByIdAsync(id, ct);
        return category is null ? null : Mapper.Map<CategoryDto>(category);
    }

    public async Task MoveAsync(long nodeId, long? newParentId, CancellationToken ct = default)
    {
        await _repository.MoveAsync(nodeId, newParentId, ct);
        await UnitOfWork.SaveChangesAsync(ct);
    }
}