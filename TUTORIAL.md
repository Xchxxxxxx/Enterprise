# EfCore.Enterprise 企业级开发框架 - 完整使用教程

## 目录
1. [框架架构概览](#1-框架架构概览)
2. [环境准备](#2-环境准备)
3. [安装框架](#3-安装框架)
4. [创建项目](#4-创建项目)
5. [领域层设计](#5-领域层设计)
6. [仓储与数据访问](#6-仓储与数据访问)
7. [应用层开发](#7-应用层开发)
8. [DTO与契约层](#8-dto与契约层)
9. [控制器与API](#9-控制器与api)
10. [树形结构](#10-树形结构)
11. [领域事件](#11-领域事件)
12. [数据验证](#12-数据验证)
13. [AutoMapper配置](#13-automapper配置)
14. [认证与授权](#14-认证与授权)
    - 14.4 [获取当前用户](#144-获取当前用户应用层)
15. [可观测性](#15-可观测性)
16. [高级特性](#16-高级特性)
    - 16.8 [规约模式](#168-规约模式specification)

---

## 1. 框架架构概览

### NuGet 包结构

```
EfCore.Enterprise (总包，安装这一个即可)
├── EfCore.Enterprise.Domain          # 领域层：实体基类、仓储接口、领域事件
├── EfCore.Enterprise.Shared          # 共享层：枚举、ApiResult、PagedResult、异常
├── EfCore.Enterprise.Infrastructure  # 基础设施层：EF Core、仓储实现、拦截器
└── EfCore.Enterprise.Application     # 应用层：CrudAppService、AutoMapper、验证
```

### 六层架构

```
┌─────────────────────────────────────────────────┐
│  06-Presentation    │  Controllers, Middleware   │
├─────────────────────────────────────────────────┤
│  04-Application     │  Services, Validators, DTO │
├─────────────────────────────────────────────────┤
│  03-Contracts       │  DTOs, Request/Response    │
├─────────────────────────────────────────────────┤
│  01-Domain          │  Entities, Events, Interfaces │
│                    │  ICurrentUser, IRepository     │
├─────────────────────────────────────────────────┤
│  05-Infrastructure  │  DbContext, Repository, EF │
├─────────────────────────────────────────────────┤
│  02-Shared          │  Enum, ApiResult, Util     │
└─────────────────────────────────────────────────┘
```

### 核心基类继承链

```
BaseEntity<TKey>              基础实体 (Id, 雪花ID)
  └── BaseAuditEntity<TKey>   审计实体 (+CreatedTime, LastModifiedTime, IsDeleted)
        └── BaseFullEntity<TKey>  完整实体 (+TenantId, RowVersion, Remark)
              ├── BaseTreeEntity<TEntity, TKey>  树形实体 (+ParentId, Children, Level, Path, Sort, IsLeaf, Name)
              └── BaseComplianceEntity<TKey>     合规实体 (+IsArchived, ArchivedTime, DataTraceCode, OperationIp)
```

---

## 2. 环境准备

### 必需环境
- **.NET 8.0 SDK** 或更高版本
- **MySQL 8.0+** (默认) 或 SQL Server / PostgreSQL
- **Visual Studio 2022** 或 VS Code

### 可选工具
- **Redis** (缓存、分布式锁)
- **Prometheus + Grafana** (监控)

---

## 3. 安装框架

### 3.1 打包框架（开发环境）

框架源码位于 `src/` 目录，修改后需重新打包：

```powershell
# 在项目根目录执行
dotnet build "EfCore.Enterprise.sln" -c Release
dotnet pack "EfCore.Enterprise.sln" -c Release -o "nupkgs"
dotnet pack "tools\EfCore.Enterprise\EfCore.Enterprise.csproj" -c Release -o "nupkgs"
dotnet pack "tools\EfCore.Enterprise.Templates\EfCore.Enterprise.Templates.csproj" -c Release -o "nupkgs"
```

### 3.1.1 更新已有项目的 NuGet 包

如果你已经有一个基于本框架的项目，修改框架源码后需要按以下步骤更新：

```powershell
# 1. 在框架根目录重新打包（如上）
# 2. 清除 NuGet 缓存（避免旧版本缓存）
dotnet nuget locals all --clear

# 3. 在已有项目根目录更新所有包
cd 你的项目目录

# 清除旧的 obj/bin
dotnet clean

# 重新恢复（--no-cache 强制不从缓存读取）
dotnet restore --no-cache

# 重新生成
dotnet build
```

> **最佳实践**：每次修改框架后，在 `src/Directory.Build.props` 中升级版本号（从 1.0.1 → 1.0.2...），然后重新打包。这样可以彻底避免缓存问题，所有项目能立即获取最新版本。

### 3.2 安装框架包

```bash
# 从本地 nupkgs 目录安装（推荐）
dotnet add package EfCore.Enterprise.Domain -s "d:\自建\项目\ef\nupkgs"
dotnet add package EfCore.Enterprise.Shared -s "d:\自建\项目\ef\nupkgs"
dotnet add package EfCore.Enterprise.Infrastructure -s "d:\自建\项目\ef\nupkgs"
dotnet add package EfCore.Enterprise.Application -s "d:\自建\项目\ef\nupkgs"

# 或安装总包（包含所有子包）
dotnet add package EfCore.Enterprise -s "d:\自建\项目\ef\nupkgs"
```

### 3.3 安装项目模板

```bash
# 安装模板（从本地 nupkg 文件）
dotnet new install "d:\自建\项目\ef\nupkgs\EfCore.Enterprise.Templates.1.0.1.nupkg"
```

### 3.4 安装 CLI 工具

```bash
# 全局安装
dotnet tool install -g EfCore.Enterprise.Cli --add-source "d:\自建\项目\ef\nupkgs"

# 验证安装
ef-cli help
```

---

## 4. 创建项目

### 方式一：dotnet new 命令

```bash
# 创建项目（默认 MySQL）
dotnet new ef-enterprise -n MyApp

# 指定数据库类型
dotnet new ef-enterprise -n MyApp Database=sqlserver
dotnet new ef-enterprise -n MyApp Database=postgresql
```

### 方式二：CLI 工具

```bash
ef-cli new MyApp
ef-cli new MyApp --db sqlserver
```

### 创建后的项目结构

```
MyApp/
├── MyApp.sln
├── 01-Domain/
│   ├── Entities/
│   │   ├── Product.cs           # BaseFullEntity 示例
│   │   ├── Category.cs          # BaseTreeEntity 树形结构
│   │   └── News.cs              # BaseEntity 简单实体
│   └── Events/
│       └── ProductPriceChangedDomainEvent.cs
├── 02-Shared/                    # 共享层
├── 03-Contracts/
│   ├── Products/Dtos/
│   ├── Categories/Dtos/
│   └── News/Dtos/
├── 04-Application/
│   ├── Products/
│   │   ├── Services/ProductService.cs
│   │   ├── Mapping/ProductProfile.cs
│   │   ├── EventHandlers/
│   │   ├── Validation/
│   │   └── Queries/
│   ├── Categories/
│   │   ├── Services/CategoryService.cs
│   │   └── Mapping/CategoryProfile.cs
│   └── News/
│       ├── Services/NewsService.cs
│       └── Mapping/NewsProfile.cs
├── 05-Infrastructure/
│   └── Data/
│       ├── AppDbContext.cs
│       ├── Configurations/
│       └── SeedData/
└── 06-Presentation/
    ├── Controllers/
    ├── Program.cs
    └── appsettings.json
```

---

## 5. 领域层设计

### 5.1 实体类型选择

| 实体类型 | 适用场景 | 包含字段 |
|----------|----------|----------|
| `BaseEntity<TKey>` | 简单实体，无审计需求 | Id (雪花ID) |
| `BaseAuditEntity<TKey>` | 需要审计的实体 | + CreatedTime, LastModifiedTime, IsDeleted (软删除) |
| `BaseFullEntity<TKey>` | 多租户、乐观锁 | + TenantId, RowVersion, Remark |
| `BaseComplianceEntity<TKey>` | 合规实体（归档/追溯） | + IsArchived, ArchivedTime, DataTraceCode, OperationIp |
| `BaseTreeEntity<TEntity>` | 树形结构 | + ParentId, Children, Level, Path, Sort, IsLeaf, Name |

> **注意**：所有主键类型 `TKey` 必须是值类型（`long`, `int`, `Guid`），默认简写 `BaseEntity` 使用 `long`。

### 5.2 创建实体

```csharp
// 01-Domain/Entities/Product.cs
using EfCore.Enterprise.Domain.Entities;

namespace MyApp.Domain.Entities;

public class Product : BaseFullEntity
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Description { get; set; }
    public long CategoryId { get; set; }

    // 领域行为
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentException("价格必须大于0");

        Price = newPrice;
        AddDomainEvent(new ProductPriceChangedDomainEvent(Id, newPrice));
    }
}
```

### 5.3 泛型实体（自定义主键类型）

```csharp
// 使用 Guid 主键
public class Order : BaseFullEntity<Guid>
{
    public string OrderNo { get; set; } = null!;
}

// 使用 int 主键  
public class Tag : BaseEntity<int>
{
    public string Name { get; set; } = null!;
}
```

---

## 6. 仓储与数据访问

### 6.1 ISuperRepository 接口

框架提供 `ISuperRepository<TEntity>` 作为统一仓储接口，包含所有 CRUD 操作：

```csharp
public interface ISuperRepository<TEntity> where TEntity : class
{
    // 查询
    IQueryable<TEntity> Query();
    Task<TEntity?> GetByIdAsync(long id);
    Task<List<TEntity>> GetAllAsync();
    Task<PagedResult<TEntity>> GetPagedAsync(Expression<Func<TEntity, bool>> predicate, PagedRequest request);

    // 增删改
    Task AddAsync(TEntity entity);
    Task AddRangeAsync(IEnumerable<TEntity> entities);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
    Task DeleteRangeAsync(IEnumerable<TEntity> entities);

    // 聚合
    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);

    // 事务
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

### 6.2 动态查询

```csharp
// 使用 Expression 构建动态查询
Expression<Func<Product, bool>> predicate = x =>
    (string.IsNullOrEmpty(keyword) || x.Name.Contains(keyword)) &&
    (!minPrice.HasValue || x.Price >= minPrice) &&
    (!maxPrice.HasValue || x.Price <= maxPrice);

var result = await _repository.GetPagedAsync(predicate, new PagedRequest
{
    PageIndex = 1,
    PageSize = 20,
    SortField = "Price",
    SortOrder = "desc"
});
```

### 6.3 自定义 DbContext

```csharp
// 05-Infrastructure/Data/AppDbContext.cs
using EfCore.Enterprise.Domain.Events;
using EfCore.Enterprise.Infrastructure.Data;
using EfCore.Enterprise.Infrastructure.Data.Interceptors;

namespace MyApp.Infrastructure.Data;

public class AppDbContext : EfCore.Enterprise.Infrastructure.Data.AppDbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    public AppDbContext(
        DbContextOptions<EfCore.Enterprise.Infrastructure.Data.AppDbContext> options,
        AuditInterceptor auditInterceptor,
        SoftDeleteInterceptor softDeleteInterceptor,
        TenantInterceptor tenantInterceptor,
        ComplianceInterceptor complianceInterceptor,
        IDomainEventBus domainEventBus)
        : base(options, auditInterceptor, softDeleteInterceptor,
               tenantInterceptor, complianceInterceptor, domainEventBus)
    {
    }
}
```

### 6.4 实体配置

```csharp
// 05-Infrastructure/Data/Configurations/ProductConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyApp.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Price).HasPrecision(18, 2);
        builder.HasIndex(x => x.CategoryId);
    }
}
```

---

## 7. 应用层开发

### 7.1 CrudAppService（零代码 CRUD）

继承 `CrudAppService` 即可获得完整的增删改查分页功能：

```csharp
// 04-Application/News/Services/NewsService.cs
using EfCore.Enterprise.Application.Crud;
using EfCore.Enterprise.Domain.Interfaces;
using AutoMapper;

namespace MyApp.Application.News.Services;

public class NewsService : CrudAppService<Domain.Entities.News, NewsDto, CreateNewsDto, UpdateNewsDto>
{
    public NewsService(ISuperRepository<Domain.Entities.News> repository, IMapper mapper)
        : base(repository, mapper)
    {
    }
}
```

**自动获得的方法**：
- `GetPageAsync(PagedRequest)` - 分页查询
- `GetByIdAsync(long)` - 根据ID查询
- `CreateAsync(TCreateDto)` - 创建
- `UpdateAsync(long, TUpdateDto)` - 更新
- `DeleteAsync(long)` - 删除（软删除）

### 7.2 自定义业务逻辑

```csharp
// 04-Application/Products/Services/ProductService.cs
using EfCore.Enterprise.Shared.Models;
using EfCore.Enterprise.Domain.Interfaces;
using System.Linq.Expressions;

namespace MyApp.Application.Products.Services;

public class ProductService
{
    private readonly ISuperRepository<Product> _repository;
    private readonly IMapper _mapper;

    public ProductService(ISuperRepository<Product> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PagedResult<ProductDto>> GetPagedListAsync(
        string? keyword, decimal? minPrice, decimal? maxPrice,
        int pageIndex = 1, int pageSize = 20)
    {
        Expression<Func<Product, bool>> predicate = x =>
            (string.IsNullOrEmpty(keyword) || x.Name.Contains(keyword)) &&
            (!minPrice.HasValue || x.Price >= minPrice.Value) &&
            (!maxPrice.HasValue || x.Price <= maxPrice.Value);

        var result = await _repository.GetPagedAsync(predicate, new PagedRequest
        {
            PageIndex = pageIndex,
            PageSize = pageSize
        });

        return new PagedResult<ProductDto>(
            _mapper.Map<List<ProductDto>>(result.Items),
            result.TotalCount,
            result.PageIndex,
            result.PageSize);
    }

    public async Task<ProductDto> UpdatePriceAsync(long id, decimal newPrice)
    {
        var product = await _repository.GetByIdAsync(id)
            ?? throw new AppException("产品不存在");

        product.UpdatePrice(newPrice);
        await _repository.UpdateAsync(product);
        await _repository.SaveChangesAsync();

        return _mapper.Map<ProductDto>(product);
    }
}
```

### 7.3 重写过滤条件

```csharp
public class MyNewsService : CrudAppService<News, NewsDto, CreateNewsDto, UpdateNewsDto>
{
    public MyNewsService(ISuperRepository<News> repository, IMapper mapper)
        : base(repository, mapper) { }

    // 重写过滤：只返回已发布的新闻
    protected override IQueryable<News> ApplyFilter(
        IQueryable<News> query, PagedRequest request)
    {
        return query.Where(x => x.Status == "Published");
    }
}
```

---

## 8. DTO与契约层

### 8.1 定义 DTO

```csharp
// 03-Contracts/Products/Dtos/ProductDto.cs
namespace MyApp.Contracts.Products.Dtos;

public class ProductDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

// 03-Contracts/Products/Dtos/CreateProductDto.cs
public class CreateProductDto
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Description { get; set; }
    public long CategoryId { get; set; }
}

// 03-Contracts/Products/Dtos/UpdateProductDto.cs
public class UpdateProductDto
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string? Description { get; set; }
}
```

### 8.2 统一响应模型

框架内置 `ApiResult<T>` 和 `PagedResult<T>`：

```csharp
// 成功响应
ApiResult<ProductDto>.Success(product);

// 分页响应
ApiResult<PagedResult<ProductDto>>.Success(pagedResult);

// 错误响应
ApiResult<ProductDto>.Fail("产品不存在", ErrorCode.NotFound);

// 控制器中直接使用
[HttpGet("{id:long}")]
public async Task<IActionResult> GetById(long id)
{
    var result = await _service.GetByIdAsync(id);
    return result is null
        ? NotFound()
        : Ok(ApiResult<ProductDto>.Success(result));
}
```

---

## 9. 控制器与API

### 9.1 标准控制器

```csharp
// 06-Presentation/Controllers/ProductsController.cs
using EfCore.Enterprise.Shared.Models;

namespace MyApp.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly NewsService _newsService;

    public ProductsController(ProductService productService, NewsService newsService)
    {
        _productService = productService;
        _newsService = newsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? keyword,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _productService.GetPagedListAsync(
            keyword, minPrice, maxPrice, pageIndex, pageSize);
        return Ok(ApiResult<PagedResult<ProductDto>>.Success(result));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _productService.GetByIdAsync(id);
        return result is null
            ? NotFound()
            : Ok(ApiResult<ProductDto>.Success(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var result = await _productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResult<ProductDto>.Success(result));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProductDto dto)
    {
        var result = await _productService.UpdateAsync(id, dto);
        return Ok(ApiResult<ProductDto>.Success(result));
    }

    [HttpPut("{id:long}/price")]
    public async Task<IActionResult> UpdatePrice(
        long id, [FromBody] UpdateProductPriceDto dto)
    {
        dto.Id = id;
        var result = await _productService.UpdatePriceAsync(dto);
        return Ok(ApiResult<ProductDto>.Success(result));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _productService.DeleteAsync(id);
        return NoContent();
    }

    // 泛型CRUD控制器（NewsService继承CrudAppService）
    [HttpGet("news")]
    public async Task<IActionResult> GetNewsList([FromQuery] PagedRequest request)
    {
        var result = await _newsService.GetPageAsync(request);
        return Ok(ApiResult<PagedResult<NewsDto>>.Success(result));
    }
}
```

---

## 10. 树形结构

### 10.1 定义树形实体

```csharp
// 01-Domain/Entities/Category.cs
using EfCore.Enterprise.Domain.Entities;

namespace MyApp.Domain.Entities;

public class Category : BaseTreeEntity<Category>
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public int SortOrder { get; set; }
}
```

### 10.2 树形仓储接口

```csharp
public interface ITreeRepository<TEntity> : ISuperRepository<TEntity>
    where TEntity : BaseTreeEntity<TEntity>
{
    Task<List<TEntity>> GetTreeAsync();
    Task<List<TEntity>> GetChildrenAsync(long parentId);
    Task<List<TEntity>> GetAncestorsAsync(long nodeId);
    Task<List<TEntity>> GetDescendantsAsync(long nodeId);
    Task MoveNodeAsync(long nodeId, long? newParentId);
    Task<int> GetDepthAsync(long nodeId);
    Task<bool> IsAncestorAsync(long ancestorId, long descendantId);
}
```

### 10.3 树形服务

```csharp
// 04-Application/Categories/Services/CategoryService.cs
namespace MyApp.Application.Categories.Services;

public class CategoryService
{
    private readonly ITreeRepository<Category> _repository;
    private readonly IMapper _mapper;

    public CategoryService(ITreeRepository<Category> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<CategoryDto>> GetTreeAsync()
    {
        var tree = await _repository.GetTreeAsync();
        return _mapper.Map<List<CategoryDto>>(tree);
    }

    public async Task MoveNodeAsync(long nodeId, long? newParentId)
    {
        await _repository.MoveNodeAsync(nodeId, newParentId);
        await _repository.SaveChangesAsync();
    }
}
```

### 10.4 树形控制器

```csharp
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ITreeRepository<Category> _repository;

    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var tree = await _repository.GetTreeAsync();
        return Ok(ApiResult<List<CategoryDto>>.Success(_mapper.Map<List<CategoryDto>>(tree)));
    }

    [HttpPut("{id:long}/move")]
    public async Task<IActionResult> MoveNode(long id, [FromQuery] long? parentId)
    {
        await _repository.MoveNodeAsync(id, parentId);
        await _repository.SaveChangesAsync();
        return Ok();
    }
}
```

---

## 11. 领域事件

### 11.1 定义领域事件

```csharp
// 01-Domain/Events/ProductPriceChangedDomainEvent.cs
using EfCore.Enterprise.Domain.Events;

namespace MyApp.Domain.Events;

public class ProductPriceChangedDomainEvent : DomainEvent
{
    public long ProductId { get; }
    public decimal NewPrice { get; }
    public DateTime OccurredAt { get; }

    public ProductPriceChangedDomainEvent(long productId, decimal newPrice)
    {
        ProductId = productId;
        NewPrice = newPrice;
        OccurredAt = DateTime.UtcNow;
    }
}
```

### 11.2 在实体中触发事件

```csharp
public class Product : BaseFullEntity
{
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentException("价格必须大于0");

        Price = newPrice;
        AddDomainEvent(new ProductPriceChangedDomainEvent(Id, newPrice));
    }
}
```

### 11.3 处理领域事件

```csharp
// 04-Application/Products/EventHandlers/ProductPriceChangedEventHandler.cs
using MediatR;
using EfCore.Enterprise.Domain.Events;
using Microsoft.Extensions.Logging;

namespace MyApp.Application.Products.EventHandlers;

public class ProductPriceChangedEventHandler
    : INotificationHandler<ProductPriceChangedDomainEvent>
{
    private readonly ILogger<ProductPriceChangedEventHandler> _logger;

    public ProductPriceChangedEventHandler(
        ILogger<ProductPriceChangedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(
        ProductPriceChangedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "产品 {ProductId} 价格已变更为 {NewPrice}",
            notification.ProductId, notification.NewPrice);

        // 可以在这里发送通知、更新缓存、记录日志等
        return Task.CompletedTask;
    }
}
```

---

## 12. 数据验证

### 12.1 FluentValidation 验证器

```csharp
// 04-Application/Products/Validation/CreateProductDtoValidator.cs
using FluentValidation;

namespace MyApp.Application.Products.Validation;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("产品名称不能为空")
            .MaximumLength(200).WithMessage("产品名称不能超过200个字符");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("价格必须大于0");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("库存不能为负数");
    }
}
```

### 12.2 在控制器中使用验证

```csharp
[HttpPost]
public async Task<IActionResult> Create(
    [FromBody] CreateProductDto dto,
    [FromServices] IValidator<CreateProductDto> validator)
{
    var validationResult = await validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        return BadRequest(ApiResult.Fail(
            validationResult.Errors.First().ErrorMessage));
    }

    var result = await _productService.CreateAsync(dto);
    return Ok(ApiResult<ProductDto>.Success(result));
}
```

---

## 13. AutoMapper配置

### 13.1 使用 BaseProfile

框架提供 `BaseProfile` 替代原始的 `Profile`，实现 `Configure()` 方法即可：

```csharp
// 04-Application/Products/Mapping/ProductProfile.cs
using AutoMapper;
using EfCore.Enterprise.Application.Mapping;

namespace MyApp.Application.Products.Mapping;

public class ProductProfile : BaseProfile
{
    protected override void Configure()
    {
        CreateMap<Product, ProductDto>();
        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>()
            .ForAllMembers(opts => opts.Condition(
                (src, dest, srcMember) => srcMember != null));
    }
}
```

### 13.2 在 DI 中注册 AutoMapper

```csharp
// Program.cs - 框架已自动注册，只需添加自己的 Profile 程序集
builder.Services.AddAutoMapper(typeof(ProductProfile).Assembly);
```

---

## 14. 认证与授权

### 14.1 JWT 配置

```json
// appsettings.json
{
  "Jwt": {
    "SecretKey": "your-256-bit-secret-key-here-must-be-long-enough",
    "Issuer": "EfCore.Enterprise",
    "Audience": "EfCore.Enterprise",
    "ExpireMinutes": 1440
  }
}
```

### 14.2 使用 JWT 服务

```csharp
[Injectable(ServiceLifetime.Singleton)]
public class AuthService
{
    private readonly IJwtService _jwtService;

    public AuthService(IJwtService jwtService)
    {
        _jwtService = jwtService;
    }

    public TokenResponse? LoginAsync(LoginRequest request)
    {
        // 验证用户名密码（自行实现）
        if (!ValidateUser(request.Username, request.Password))
            return null;

        var accessToken = _jwtService.GenerateAccessToken(
            userId: 1,
            username: request.Username,
            roles: new[] { "Admin" },
            permissions: new[] { "System.User.Query" });

        var refreshToken = _jwtService.GenerateRefreshToken();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 86400
        };
    }
}

[HttpPost("login")]
public IActionResult Login([FromBody] LoginRequest request)
{
    var result = _authService.LoginAsync(request);
    return result is null
        ? Unauthorized(ApiResult.Fail("用户名或密码错误"))
        : Ok(ApiResult<TokenResponse>.Success(result));
}
```

### 14.3 权限控制

```csharp
[Authorize]
[HttpGet("profile")]
public async Task<IActionResult> GetProfile()
{
    var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    return Ok(ApiResult<string>.Success($"当前用户ID: {userId}"));
}
```

### 14.4 获取当前用户（应用层）

框架提供了 `ICurrentUser` 接口，且 `BaseService` 和 `CrudAppService` 基类已自动注入，**继承即可用**：

```csharp
// 继承 BaseService 或 CrudAppService，自动获得 CurrentUser
public class OrderService : CrudAppService<Order, OrderDto, CreateOrderDto, UpdateOrderDto>
{
    public OrderService(
        ISuperRepository<Order> repo,
        IMapper mapper,
        ICurrentUser currentUser)
        : base(repo, mapper, currentUser) { }

    public async Task PlaceOrderAsync()
    {
        // 直接使用，无需手动注入
        var userId = CurrentUser.UserId;       // 当前用户ID
        var userName = CurrentUser.UserName;   // 当前用户名
        var roles = CurrentUser.Roles;         // 角色列表
        var permissions = CurrentUser.Permissions; // 权限列表
        var tenantId = CurrentUser.TenantId;   // 租户ID
        var isAuth = CurrentUser.IsAuthenticated;  // 是否已认证

        if (!permissions.Contains("order.create"))
            throw new ForbiddenException("无权限创建订单");
    }
}
```

`ICurrentUser` 属性一览：

| 属性 | 类型 | 来源 Claim |
|------|------|-----------|
| `UserId` | `long?` | `ClaimTypes.NameIdentifier` / `sub` / `userId` |
| `UserName` | `string?` | `ClaimTypes.Name` / `unique_name` / `username` |
| `Roles` | `IEnumerable<long>` | `ClaimTypes.Role` / `roles`（逗号分隔） |
| `Permissions` | `IEnumerable<string>` | `permissions` / `scope`（逗号分隔） |
| `TenantId` | `long?` | `tenant_id` / `tenantId` |
| `IsAuthenticated` | `bool` | `User.Identity.IsAuthenticated` |
| `ClientIp` | `string?` | `HttpContext.Connection.RemoteIpAddress` |

---

## 15. 可观测性

### 15.1 OpenTelemetry 配置

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddPrometheusExporter())
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddConsoleExporter());
```

### 15.2 Serilog 日志

```csharp
// Program.cs - 框架已封装，一行搞定
builder.AddEfCoreSerilog();
```

框架默认 Console 输出自带颜色区分日志级别：

| 级别 | 颜色 | 示例 |
|------|------|------|
| Debug | 灰色 | `[18:30:07 DBG] 调试信息` |
| Information | 绿色 | `[18:30:07 INF] 请求开始` |
| Warning | 黄色 | `[18:30:07 WRN] N+1查询告警` |
| Error | 红色 | `[18:30:07 ERR] 未处理异常` |
| Fatal | 深红 | `[18:30:07 FTL] 致命错误` |

如需自定义，在 `appsettings.json` 中配置 `Serilog:WriteTo`，框架就不会覆盖你的配置。

### 15.3 健康检查

```csharp
// 框架已内置健康检查端点
// GET /health
// 返回: { "status": "Healthy", "database": "Healthy", "memory": "Healthy" }
```

---

## 16. 高级特性

### 16.1 多租户

```csharp
// 实体自动继承 TenantId 过滤
public class Product : BaseFullEntity
{
    // TenantId 自动从 ITenantProvider 获取
}

// 自定义租户提供者
public class MyTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContext;

    public long? GetTenantId()
    {
        var claim = _httpContext.HttpContext?.User
            .FindFirst("TenantId");
        return claim != null ? long.Parse(claim.Value) : null;
    }
}
```

### 16.2 缓存服务

```csharp
public class ProductService
{
    private readonly ICacheService _cache;

    public async Task<ProductDto?> GetByIdAsync(long id)
    {
        var cacheKey = $"product:{id}";
        var cached = await _cache.GetAsync<ProductDto>(cacheKey);
        if (cached != null) return cached;

        var product = await _repository.GetByIdAsync(id);
        var dto = _mapper.Map<ProductDto>(product);

        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10));
        return dto;
    }
}
```

### 16.3 分布式锁

```csharp
public async Task DeductStockAsync(long productId, int quantity)
{
    var lockKey = $"stock:lock:{productId}";
    await using var @lock = await _distributedLock
        .AcquireAsync(lockKey, TimeSpan.FromSeconds(10));

    var product = await _repository.GetByIdAsync(productId);
    product!.Stock -= quantity;
    await _repository.SaveChangesAsync();
}
```

### 16.4 雪花ID生成

```csharp
// 框架自动使用雪花ID作为主键
// 无需手动设置，BaseEntity 构造函数自动生成
var product = new Product { Name = "测试" };
// product.Id = 1234567890123456789 (雪花ID)
```

### 16.5 性能监控

```csharp
// 框架内置性能监控端点
// GET /metrics
// 返回 Prometheus 格式的指标数据
```

### 16.6 操作日志

框架自动记录所有数据变更操作：

```csharp
// 框架内置 OpLog 实体，自动记录：
// - 操作类型 (新增/修改/删除)
// - 操作时间
// - 操作人
// - 变更内容
```

### 16.7 代码生成器

```bash
# 使用 CLI 生成完整模块
ef-cli generate module Product --web

# 交互式生成
ef-cli generate interactive

# 生成数据库迁移
ef-cli generate migration

# 生成单元测试
ef-cli generate test ProductService
```

### 16.8 规约模式（Specification）

把查询条件封装成可组合的对象，**避免仓库方法爆炸**。

```csharp
using EfCore.Enterprise.Domain.Specifications;

// 基础查询：已支付 + 最近7天 + 金额>1000
var spec = Spec<Order>.From(o => o.Status == "paid")
    .Where(o => o.CreatedTime > DateTime.Today.AddDays(-7))
    .Where(o => o.Amount > 1000)
    .SortByDesc(o => o.CreatedTime)
    .Page(1, 20);

// 一次调用，自动处理过滤 + 排序 + 分页
var result = await _repository.QueryAsync(spec);

// 条件复用
public class PaidOrderSpec : Spec<Order>
{
    public PaidOrderSpec() => Where(o => o.Status == "paid");
}

public class LargeOrderSpec : Spec<Order>
{
    public LargeOrderSpec() => Where(o => o.Amount > 10000);
}

// 组合使用
var combined = new PaidOrderSpec().And(new LargeOrderSpec()).Page(1, 20);
var orders = await _repository.QueryAsync(combined);

// 条件可选
var spec2 = Spec<Order>.From(o => o.Status == "paid")
    .AndIf(customerId.HasValue, o => o.CustomerId == customerId.Value)
    .Page(page, 20);

// 列表查询（不分页）
var list = await _repository.ListAsync(spec);

// 计数
var count = await _repository.CountAsync(spec);
```

| 方法 | 链式调用 |
|------|---------|
| `Spec<T>.From(expr)` | 创建规约 |
| `.Where(expr)` | 追加 AND 条件 |
| `.AndIf(condition, expr)` | 条件满足时才追加 |
| `.And(spec)` / `.Or(spec)` | 组合另一个规约 |
| `.SortBy(expr)` | 升序 |
| `.SortByDesc(expr)` | 降序 |
| `.ThenSortBy(expr)` | 二级排序 |
| `.Page(index, size)` | 分页 |
| `.Include(expr)` | 贪婪加载导航属性 |

---

## 附录

### A. 数据库连接字符串

```json
// MySQL
"DefaultConnection": "Server=localhost;Database=MyApp;User=root;Password=123456;"

// SQL Server
"DefaultConnection": "Server=localhost;Database=MyApp;User Id=sa;Password=123456;"

// PostgreSQL
"DefaultConnection": "Host=localhost;Database=MyApp;Username=postgres;Password=123456;"
```

### B. 常用命令速查

```bash
# 框架打包
dotnet build EfCore.Enterprise.sln -c Release
dotnet pack EfCore.Enterprise.sln -c Release -o nupkgs
dotnet pack tools\EfCore.Enterprise\EfCore.Enterprise.csproj -c Release -o nupkgs
dotnet pack tools\EfCore.Enterprise.Templates\EfCore.Enterprise.Templates.csproj -c Release -o nupkgs

# 框架安装
dotnet add package EfCore.Enterprise -s "d:\自建\项目\ef\nupkgs"

# 模板安装
dotnet new install "d:\自建\项目\ef\nupkgs\EfCore.Enterprise.Templates.1.0.1.nupkg"

# 创建项目
dotnet new ef-enterprise -n MyApp

# 数据库迁移（在 Infrastructure 目录执行）
cd 05-Infrastructure
dotnet ef migrations add InitialCreate
dotnet ef database update

# 启动项目
dotnet run --project 06-Presentation

# CLI 工具安装
dotnet tool install -g EfCore.Enterprise.Cli --add-source "d:\自建\项目\ef\nupkgs"

# CLI 工具
ef-cli new MyApp
ef-cli generate module Product
ef-cli dev
```

### C. 异常处理

```csharp
// 框架内置异常类型
throw new AppException("业务异常");
throw new NotFoundException("实体不存在");
throw new UnauthorizedException("未授权访问");

// 全局异常中间件自动处理并返回统一格式
// { "success": false, "message": "业务异常", "errorCode": 400 }
```

### D. 依赖注入服务清单

| 服务 | 生命周期 | 说明 |
|------|----------|------|
| `ISuperRepository<T>` | Scoped | 泛型仓储（CRUD + 分页） |
| `ITreeRepository<T>` | Scoped | 树形仓储（树查询、移动节点） |
| `IUnitOfWork` | Scoped | 工作单元（事务管理） |
| `ICacheService` | Scoped | 缓存服务（Memory/Redis） |
| `IDistributedLockService` | Scoped | 分布式锁 |
| `IJwtService` | Singleton | JWT令牌生成与验证 |
| `IDomainEventBus` | Singleton | 领域事件总线（MediatR） |
| `IOpLogService` | Scoped | 操作日志服务 |
| `DevModeService` | Singleton | 开发模式（自动迁移/种子数据） |
| `GrayReleaseRuleManager` | Singleton | 灰度发布规则管理 |
| `DataSeeder` | Singleton | 种子数据初始化 |
| `TransactionManager` | Scoped | 事务管理器（封装提交/回滚） |