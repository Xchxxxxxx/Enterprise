using EfCore.Enterprise.Application.Extensions;
using EfCore.Enterprise.Infrastructure.Configuration;
using EfCore.Enterprise.Infrastructure.Data;
using EfCore.Enterprise.Infrastructure.Data.Interceptors;
using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Domain.Events;
using EfCore.Enterprise.Shared.DependencyInjection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddEfCoreSerilog();

builder.Services.AddEfCoreEnterprise(builder.Configuration);

builder.Services.AddScoped<AppDbContext>(sp =>
{
    var options = sp.GetRequiredService<DbContextOptions<AppDbContext>>();
    return new MyApp.Infrastructure.Data.AppDbContext(
        options,
        sp.GetRequiredService<AuditInterceptor>(),
        sp.GetRequiredService<SoftDeleteInterceptor>(),
        sp.GetRequiredService<TenantInterceptor>(),
        sp.GetRequiredService<ComplianceInterceptor>(),
        sp.GetRequiredService<IDomainEventBus>());
});

var projectAssemblies = new[]
{
    typeof(BaseEntity).Assembly,
    typeof(InjectableAttribute).Assembly,
    typeof(MyApp.Infrastructure.Data.AppDbContext).Assembly,
    typeof(MyApp.Application.Products.Services.ProductService).Assembly,
    typeof(Program).Assembly
};

builder.Services.AddEfCoreAutoInject(projectAssemblies);

var app = builder.Build();

app.UseEfCorePipeline(app.Environment.IsDevelopment());

app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

app.Run();