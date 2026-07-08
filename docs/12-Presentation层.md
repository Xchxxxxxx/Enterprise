# 第12章：Web 层配置与控制器

> **架构变更**：框架不再包含 Presentation 层。控制器基类（`BaseApiController`、`CrudController`）已迁移到 Application 层，中间件和配置扩展已迁移到 Infrastructure 层。用户在模板的 Presentation 层中编写自己的 Controller，使用框架提供的基类。

## 12.1 统一响应格式

### ApiResult（单个对象 / 简单结果）

```csharp
// 成功
return ApiResult.Ok();
return ApiResult.Ok("操作成功");
return ApiResult<ProductDto>.Ok(productDto);

// 失败
return ApiResult.Fail("操作失败");
return ApiResult<ProductDto>.Fail("产品不存在", ErrorCode.NotFound);

// 返回的数据格式
{
  "success": true,
  "code": null,
  "message": "操作成功",
  "data": { ... },
  "traceId": "abc123"
}
```

### PagedResult（分页数据）

```csharp
// 成功
return PagedResult<ProductDto>.Ok(items, totalCount, pageIndex, pageSize);

// 返回的数据格式
{
  "success": true,
  "code": null,
  "message": null,
  "data": {
    "items": [ ... ],
    "totalCount": 100,
    "pageIndex": 1,
    "pageSize": 20,
    "totalPages": 5,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "traceId": "abc123"
}
```

## 12.2 框架提供的控制器基类

### BaseApiController（基础控制器）

位于 `EfCore.Enterprise.Application.Controllers` 命名空间，提供 `Success()`/`Fail()` 快捷方法：

```csharp
using EfCore.Enterprise.Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MyController : BaseApiController
{
    [HttpGet("{id}")]
    public IActionResult GetById(long id)
    {
        // Success() 快捷方法
        return Success(data);
        // 等同于 ApiResult<T>.Ok(data)

        // Fail() 快捷方法
        return Fail("数据不存在", 404);
        // 等同于 ApiResult<T>.Fail("数据不存在", 404)
    }
}
```

### CrudController（泛型CRUD控制器）

提供零代码 CRUD 端点，位于 `EfCore.Enterprise.Application.Controllers`：

```csharp
using EfCore.Enterprise.Application.Controllers;
using EfCore.Enterprise.Application.Crud;

public class ProductsController : CrudController<Product, ProductDto, CreateProductDto, UpdateProductDto>
{
    public ProductsController(ICrudAppService<Product, ProductDto, CreateProductDto, UpdateProductDto> appService)
        : base(appService)
    {
    }
}
```

内置端点：
- `GET /api/products` — 分页查询
- `GET /api/products/{id}` — 查询单个
- `POST /api/products` — 创建
- `PUT /api/products/{id}` — 更新
- `DELETE /api/products/{id}` — 删除
- `DELETE /api/products/batch` — 批量删除

## 12.3 自定义 Controller 规范

```csharp
using Asp.Versioning;
using EfCore.Enterprise.Application.Controllers;
using EfCore.Enterprise.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CompanyName.Application.Products.Dtos;
using CompanyName.Application.Products.Services;

namespace CompanyName.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ProductsController : BaseApiController
{
    private readonly ProductService _productService;

    public ProductsController(ProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [Permission("Product.List")]
    public async Task<IActionResult> GetPage([FromQuery] GetProductListQuery query)
    {
        var result = await _productService.GetPagedListAsync(query);
        return Success(result);
    }

    [HttpGet("{id:long}")]
    [Permission("Product.View")]
    public async Task<IActionResult> GetById(long id)
    {
        var product = await _productService.GetByIdAsync(id);
        return product is null
            ? Fail("产品不存在", 404)
            : Success(product);
    }

    [HttpPost]
    [Permission("Product.Create")]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var product = await _productService.CreateAsync(dto);
        return Success(product, "创建成功");
    }

    [HttpDelete("{id:long}")]
    [Permission("Product.Delete")]
    public async Task<IActionResult> Delete(long id)
    {
        await _productService.DeleteAsync(id);
        return Success("删除成功");
    }
}
```

## 12.4 RESTful API 命名规范

| 操作 | HTTP 方法 | URL | 示例 |
|------|----------|-----|------|
| 分页查询 | `GET` | `/api/v1/products` | `GET /api/v1/products?keyword=手机&pageIndex=1&pageSize=20` |
| 查询单个 | `GET` | `/api/v1/products/{id}` | `GET /api/v1/products/123` |
| 创建 | `POST` | `/api/v1/products` | `POST /api/v1/products` |
| 更新全部 | `PUT` | `/api/v1/products/{id}` | `PUT /api/v1/products/123` |
| 更新部分 | `PATCH` | `/api/v1/products/{id}` | `PATCH /api/v1/products/123` |
| 删除 | `DELETE` | `/api/v1/products/{id}` | `DELETE /api/v1/products/123` |
| 批量删除 | `DELETE` | `/api/v1/products/batch` | `DELETE /api/v1/products/batch?ids=1,2,3` |
| 执行操作 | `POST` | `/api/v1/products/{id}/action` | `POST /api/v1/orders/123/pay` |

## 12.5 服务注册（Program.cs）

```csharp
using EfCore.Enterprise.Application.Extensions;
using EfCore.Enterprise.Infrastructure.Configuration;
using EfCore.Enterprise.Infrastructure.Data;
using EfCore.Enterprise.Infrastructure.Data.Interceptors;
using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Domain.Events;
using EfCore.Enterprise.Shared.DependencyInjection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Serilog 日志
builder.AddEfCoreSerilog();

// 一键注册（Swagger + JWT + OpenTelemetry + Cors + 基础设施 + 应用层）
builder.Services.AddEfCoreEnterprise(builder.Configuration);

// 覆盖 AppDbContext 注册（使用项目自定义的子类）
builder.Services.AddScoped<AppDbContext>(sp =>
{
    var options = sp.GetRequiredService<DbContextOptions<AppDbContext>>();
    return new YourProject.Infrastructure.Data.AppDbContext(
        options,
        sp.GetRequiredService<AuditInterceptor>(),
        sp.GetRequiredService<SoftDeleteInterceptor>(),
        sp.GetRequiredService<TenantInterceptor>(),
        sp.GetRequiredService<ComplianceInterceptor>(),
        sp.GetRequiredService<IDomainEventBus>());
});

// 自动注入（[Injectable] + FluentValidation + MediatR + AutoMapper）
var assemblies = new[]
{
    typeof(BaseEntity).Assembly,
    typeof(InjectableAttribute).Assembly,
    typeof(YourProject.Infrastructure.Data.AppDbContext).Assembly,
    typeof(YourProject.Application.Products.Services.ProductService).Assembly,
    typeof(Program).Assembly
};
builder.Services.AddEfCoreAutoInject(assemblies);

var app = builder.Build();

// 一键配置中间件管道（含 Swagger + CORS + 中间件 + OpenTelemetry + JWT）
app.UseEfCorePipeline(app.Environment.IsDevelopment());

// 健康检查端点
app.MapHealthChecks("/health");

// 种子数据初始化
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

app.Run();
```

### 功能按需拆分

如果不需要框架的全部功能，可以分步注册：

```csharp
// 只注册需要的功能
builder.Services.AddEfCoreControllers();
builder.Services.AddEfCoreSwagger(builder.Configuration);
builder.Services.AddEfCoreJwt(builder.Configuration);
builder.Services.AddEfCoreCors();
builder.Services.AddInfrastructureServices(connectionString);
builder.Services.AddApplicationServices();

// 中间件也可按需配置
app.UseEfCoreMiddleware();        // 仅注册中间件，不含Swagger
app.UseAuthentication();
app.UseAuthorization();
```

## 12.6 框架内置中间件

所有中间件位于 `EfCore.Enterprise.Infrastructure.Middleware` 命名空间：

| 中间件 | 功能 | 启用方式 |
|--------|------|---------|
| `GlobalExceptionMiddleware` | 全局异常处理，统一捕获 `AppException` 和未处理异常，返回标准错误格式 | `UseEfCoreMiddleware()` 自动注册 |
| `RequestLogMiddleware` | 请求日志记录，记录请求路径、耗时、状态码、TraceId | `UseEfCoreMiddleware()` 自动注册 |
| `RateLimitMiddleware` | 限流控制，结合 `[RateLimit]` 特性生效 | `UseEfCoreMiddleware()` 自动注册 |
| `IdempotencyMiddleware` | 幂等性保证，基于 `Idempotency-Key` 请求头防止重复提交 | `UseEfCoreMiddleware()` 自动注册 |
| `PerformanceMiddleware` | 性能监控，采集请求耗时、慢请求告警 | `UseEfCoreMiddleware()` 自动注册 |
| `OpLogMiddleware` | 操作日志记录，自动记录用户操作并保存到数据库 | `UseEfCoreMiddleware()` 自动注册 |
| `GrayReleaseMiddleware` | 灰度发布策略，按百分比和用户白名单分配流量 | `app.UseEfCoreGrayRelease()` 单独注册 |
| `ValidationFilterMiddleware` | 参数验证过滤器，在进入 Controller 前自动验证 DTO | `app.UseEfCoreValidation()` 单独注册 |

调用 `app.UseEfCoreMiddleware()` 或 `app.UseEfCorePipeline()` 即可自动注册前六个基础中间件（按正确顺序）。

## 12.7 各中间件详细说明

### GlobalExceptionMiddleware（全局异常处理）

自动捕获所有异常，统一返回 JSON 格式：

```json
{
  "success": false,
  "code": "NOT_FOUND",
  "message": "产品不存在",
  "traceId": "abc123"
}
```

支持的异常类型：

| 异常 | HTTP 状态码 |
|------|-------------|
| `NotFoundException` | 404 |
| `UnauthorizedException` | 401 |
| `ForbiddenException` | 403 |
| `ValidationException` | 400 |
| `RateLimitExceededException` | 429 |
| `BusinessException` | 400 |
| 其他异常 | 500 |

### RequestLogMiddleware（请求日志）

框架自动记录每个请求的日志：

```
2026-07-06 10:00:00 [trace:abc123] INFO  GET /api/v1/products?pageIndex=1&pageSize=20 → 200 OK (45ms)
2026-07-06 10:00:01 [trace:abc123] INFO  POST /api/v1/products → 201 Created (120ms)
2026-07-06 10:00:02 [trace:abc123] INFO  PUT /api/v1/products/123 → 200 OK (80ms)
2026-07-06 10:00:03 [trace:abc123] INFO  DELETE /api/v1/products/123 → 200 OK (30ms)
```

### RateLimitMiddleware（限流）

结合 `[RateLimit]` 特性使用：

```csharp
// 每分钟最多 100 次
[RateLimit(100, 60)]
[HttpPost]
public async Task<ApiResult> SendSms(SendSmsRequest request)
{
    // ...
}

// 每秒最多 10 次（防暴力破解）
[RateLimit(10, 1)]
[HttpPost]
public async Task<ApiResult<LoginResult>> Login(LoginRequest request)
{
    // ...
}
```

超限自动返回 429 状态码和 `RateLimitExceededException`。

### IdempotencyMiddleware（幂等性）

客户端在请求头携带 `Idempotency-Key`（UUID）：

```http
POST /api/order/create
Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000
Content-Type: application/json

{ "orderNo": "ORD-001", "amount": 99.99 }
```

同一个 `Idempotency-Key` 的请求，只有第一次真正执行，后续返回缓存结果。有效期默认 24 小时。

### PerformanceMiddleware（性能监控）

自动采集：
- 请求总数和每秒QPS
- 请求耗时分布
- 慢请求告警（默认 > 500ms）
- 错误率统计

通过 `IPerformanceMetrics` 获取指标：

```csharp
[Injectable]
public class DashboardService : BaseService
{
    private readonly IPerformanceMetrics _metrics;

    public DashboardService(IPerformanceMetrics metrics)
    {
        _metrics = metrics;
    }

    public DashboardData GetData()
    {
        return new DashboardData
        {
            RequestsPerSecond = _metrics.GetRequestsPerSecond(),
            AverageResponseTime = _metrics.GetAverageResponseTime(),
            ErrorRate = _metrics.GetErrorRate()
        };
    }
}
```

### OpLogMiddleware（操作日志）

结合 `[AuditLog]` 特性使用：

```csharp
[AuditLog("删除产品", Module = "Product")]
[Permission("Product.Delete")]
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(long id)
{
    await _productService.DeleteAsync(id);
    return Success("删除成功");
}
```

自动记录：操作人、操作时间、IP地址、操作内容。保存到 `OpLog` 表。

### GrayReleaseMiddleware（灰度发布）

需要手动注册 `GrayReleaseRuleManager` 并添加规则：

```csharp
builder.Services.AddSingleton<GrayReleaseRuleManager>();

var app = builder.Build();

app.UseEfCoreGrayRelease();

// 添加灰度规则：/api/v2 路径 10% 流量进入灰度版本
var ruleManager = app.Services.GetRequiredService<GrayReleaseRuleManager>();
ruleManager.AddRule("/api/v2", 0.1, "v2", new List<string> { "admin", "test" });
```

支持：
- 按百分比分流
- 用户白名单（特定用户永远走灰度）
- 设置版本头 `X-Api-Version` 供网关路由

## 12.8 中间件执行顺序

`app.UseEfCorePipeline()` 按以下顺序注册：

```
1. Swagger / SwaggerUI（仅开发环境）
2. CORS
3. Serilog Request Logging
4. RequestLogMiddleware
5. RateLimitMiddleware
6. IdempotencyMiddleware
7. PerformanceMiddleware
8. OpLogMiddleware
9. GlobalExceptionMiddleware
10. OpenTelemetry Prometheus Scraping
11. HTTPS 重定向（仅非开发环境）
12. Authentication
13. Authorization
```

## 12.9 DevMode 开发模式

框架提供开发模式支持，可以在开发环境下开启一些便利功能：

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDevMode();
}

var app = builder.Build();
if (builder.Environment.IsDevelopment())
{
    app.UseDevMode();
}
```

DevMode 提供：
- 更详细的错误信息
- 开发工具页面
- 更容易调试

## 12.10 配置文件说明

框架所有配置通过 `appsettings.json` 读取，各功能对应的配置项：

| 功能 | 配置节 | 说明 |
|------|--------|------|
| 数据库 | `ConnectionStrings:DefaultConnection` | 连接字符串 |
| JWT 认证 | `Jwt:*` | 密钥、签发者、过期时间 |
| 雪花ID | `Snowflake:*` | 工作器ID、数据中心ID |
| Redis 缓存 | `Redis:Connection` | Redis 连接字符串 |
| OpenTelemetry | `OpenTelemetry:Endpoint` | 导出地址 |
| 性能监控 | `Monitoring:*` | 慢 SQL 阈值、N+1 检测阈值 |
| 日志 | `Serilog` | Serilog 配置 |

**完整配置示例和说明**请参阅 [附录 § E. 完整配置项参考](附录.md#e-完整配置项参考)。

## 12.11 功能模块化注册扩展方法

框架将所有功能拆分为独立的扩展方法，可以按需注册：

| 扩展方法 | 功能 | 所在命名空间 |
|----------|------|-------------|
| `builder.AddEfCoreSerilog()` | 集成 Serilog 日志 | `EfCore.Enterprise.Infrastructure.Configuration` |
| `builder.Services.AddEfCoreControllers()` | 添加控制器 + 全局配置 | `EfCore.Enterprise.Infrastructure.Configuration` |
| `builder.Services.AddEfCoreSwagger()` | 配置 Swagger | `EfCore.Enterprise.Infrastructure.Configuration` |
| `builder.Services.AddEfCoreJwt()` | 配置 JWT 认证 | `EfCore.Enterprise.Infrastructure.Configuration` |
| `builder.Services.AddEfCoreOpenTelemetry()` | 配置 OpenTelemetry 监控 | `EfCore.Enterprise.Infrastructure.Configuration` |
| `builder.Services.AddEfCoreCors()` | 配置 CORS | `EfCore.Enterprise.Infrastructure.Configuration` |
| `app.UseEfCorePipeline()` | 一键配置完整管道 | `EfCore.Enterprise.Application.Extensions` |
| `app.UseEfCoreMiddleware()` | 仅注册中间件 | `EfCore.Enterprise.Infrastructure.Configuration` |
| `app.UseEfCoreGrayRelease()` | 注册灰度发布中间件 | `EfCore.Enterprise.Infrastructure.Configuration` |
| `app.UseEfCoreValidation()` | 注册参数验证过滤器 | `EfCore.Enterprise.Infrastructure.Configuration` |

## 12.12 完整 Program.cs 示例（手动模式）

如果不想使用框架的 `AddEfCoreEnterprise` 一键注册，可以手动配置：

```csharp
using EfCore.Enterprise.Application.Extensions;
using EfCore.Enterprise.Infrastructure.Configuration;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("未配置连接字符串");

    // 添加框架基础设施服务
    builder.Services.AddInfrastructureServices(
        connectionString,
        enableRedis: true,
        redisConnection: builder.Configuration["Redis:Connection"],
        enableHangfire: true,
        modelCachePath: "./model_cache",
        complianceLogPath: "./compliance_logs");

    // 添加应用服务
    builder.Services.AddApplicationServices();

    // API 版本控制
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new Asp.Versioning.UrlSegmentApiVersionReader();
    });

    // Swagger
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "EfCore.Enterprise API",
            Version = "v1"
        });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT token: Bearer {token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // 添加控制器
    builder.Services.AddControllers();

    // 添加 OpenTelemetry
    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics => metrics.AddPrometheusExporter())
        .WithTracing(tracing => tracing.AddConsoleExporter());

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    // 开放 Prometheus 指标端点
    app.UseOpenTelemetryPrometheusScrapingEndpoint();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用启动失败");
}
finally
{
    Log.CloseAndFlush();
}
```