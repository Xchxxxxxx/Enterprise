using System.Diagnostics;
using System.Reflection;
using System.Text;
using EfCore.Enterprise.Application.Extensions;
using EfCore.Enterprise.Application.Mapping;
using EfCore.Enterprise.Infrastructure.Configuration;
using EfCore.Enterprise.Presentation.Middleware;
using EfCore.Enterprise.Shared.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var serviceName = typeof(Program).Assembly.GetName().Name;
var serviceVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService(serviceName: serviceName ?? "EfCore.Enterprise", serviceVersion: serviceVersion);
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
              .AddHttpClientInstrumentation()
              .AddRuntimeInstrumentation()
              .AddProcessInstrumentation()
              .AddMeter("EfCore.Enterprise")
              .AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();

        var otelEndpoint = builder.Configuration["OpenTelemetry:Endpoint"];
        if (!string.IsNullOrEmpty(otelEndpoint))
        {
            tracing.AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(otelEndpoint);
            });
        }
        else
        {
            tracing.AddConsoleExporter();
        }
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EfCore.Enterprise API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Bearer Token，请输入: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddHealthChecks();

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers["Token-Expired"] = "true";
                }
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var redisConnection = builder.Configuration.GetConnectionString("Redis");
var enableRedis = !string.IsNullOrEmpty(redisConnection);
var enableHangfire = builder.Configuration.GetValue<bool>("Hangfire:Enabled");

var projectAssemblies = new[]
{
    typeof(EfCore.Enterprise.Domain.Entities.BaseEntity).Assembly,
    typeof(EfCore.Enterprise.Shared.DependencyInjection.InjectableAttribute).Assembly,
    typeof(EfCore.Enterprise.Infrastructure.Data.AppDbContext).Assembly,
    typeof(EfCore.Enterprise.Application.Crud.CrudAppService<,,,>).Assembly,
    typeof(Program).Assembly
};

builder.Services.AddInjectables(projectAssemblies);
builder.Services.AddByConvention(projectAssemblies);

var coreModule = new CoreModule(
    connectionString!,
    enableRedis: enableRedis,
    redisConnection: redisConnection,
    enableHangfire: enableHangfire);

coreModule.ConfigureServices(builder.Services, builder.Configuration);

builder.Services.AddModules(builder.Configuration,
    typeof(CoreModule).Assembly,
    typeof(CachingModule).Assembly,
    typeof(BackgroundJobsModule).Assembly);

builder.Services.AddFluentValidationAuto(projectAssemblies);
builder.Services.AddMediatRAuto(projectAssemblies);
builder.Services.AddAutoMapperAuto(projectAssemblies);

builder.Services.AddSingleton<GrayReleaseRuleManager>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseSerilogRequestLogging();

app.UseMiddleware<RequestLogMiddleware>();
app.UseMiddleware<RateLimitMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();
app.UseMiddleware<GrayReleaseMiddleware>();
app.UseMiddleware<PerformanceMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapMetrics();
app.MapHealthChecks("/health");

var grayRuleManager = app.Services.GetRequiredService<GrayReleaseRuleManager>();
grayRuleManager.AddRule("/api/v2/", 0.2, grayVersion: "v2.0");

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<EfCore.Enterprise.Infrastructure.Data.DataSeeder>();
    await seeder.SeedAsync();
}

app.Run();