using EfCore.Enterprise.Application.Extensions;
using EfCore.Enterprise.Infrastructure.Configuration;
using EfCore.Enterprise.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddEfCoreSerilog();

builder.Services.AddEfCoreEnterprise<CompanyName.Infrastructure.Data.AppDbContext>(builder.Configuration);

builder.Services.AddEfCoreAutoInject(
    typeof(CompanyName.Infrastructure.Data.AppDbContext).Assembly,
    typeof(Program).Assembly);

var app = builder.Build();

app.UseEfCorePipeline(app.Environment.IsDevelopment());
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.InitializeAsync();
}

app.Run();