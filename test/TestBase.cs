using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Infrastructure.Data;
using EfCore.Enterprise.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EfCore.Enterprise.Test;

public abstract class UnitTestBase<TService> where TService : class
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly TService Service;
    protected readonly Mock<ISuperRepository<BaseEntity<long>>> MockRepo;
    protected readonly Mock<ISuperRepository<SysUser>> MockUserRepo;
    protected readonly Mock<ISuperRepository<SysRole>> MockRoleRepo;
    protected readonly Mock<ISuperRepository<OpLog>> MockOpLogRepo;
    protected readonly Mock<ISuperRepository<SysPermission>> MockPermissionRepo;
    protected readonly Mock<AppDbContext> MockDbContext;
    protected readonly Mock<JwtService> MockJwtService;
    protected readonly Mock<ResilienceService> MockResilienceService;

    private readonly Dictionary<Type, Mock> _mocks = new();

    protected UnitTestBase()
    {
        var services = new ServiceCollection();

        MockRepo = new Mock<ISuperRepository<BaseEntity<long>>>();
        MockUserRepo = new Mock<ISuperRepository<SysUser>>();
        MockRoleRepo = new Mock<ISuperRepository<SysRole>>();
        MockOpLogRepo = new Mock<ISuperRepository<OpLog>>();
        MockPermissionRepo = new Mock<ISuperRepository<SysPermission>>();
        MockDbContext = new Mock<AppDbContext>();
        MockJwtService = new Mock<JwtService>();
        MockResilienceService = new Mock<ResilienceService>();

        SetupMocks();

        services.AddSingleton(MockRepo.Object);
        services.AddSingleton(MockUserRepo.Object);
        services.AddSingleton(MockRoleRepo.Object);
        services.AddSingleton(MockOpLogRepo.Object);
        services.AddSingleton(MockPermissionRepo.Object);
        services.AddSingleton(MockDbContext.Object);
        services.AddSingleton(MockJwtService.Object);
        services.AddSingleton(MockResilienceService.Object);

        services.AddScoped<TService>();

        ServiceProvider = services.BuildServiceProvider();
        Service = ServiceProvider.GetRequiredService<TService>();
    }

    protected virtual void SetupMocks() { }

    protected Mock<T> GetMock<T>() where T : class
    {
        if (_mocks.TryGetValue(typeof(T), out var mock))
            return (Mock<T>)mock;

        var newMock = new Mock<T>();
        _mocks[typeof(T)] = newMock;
        return newMock;
    }

    protected Mock<DbSet<T>> CreateDbSetMock<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mock = new Mock<DbSet<T>>();
        mock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
        return mock;
    }
}

public abstract class IntegrationTestBase : IDisposable
{
    protected readonly HttpClient HttpClient;
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly IServiceProvider ServiceProvider;

    protected IntegrationTestBase(Action<IServiceCollection>? configureServices = null)
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

                    configureServices?.Invoke(services);
                });
            });

        HttpClient = Factory.CreateClient();
        ServiceProvider = Factory.Services;
    }

    protected TService GetService<TService>() where TService : notnull
        => ServiceProvider.GetRequiredService<TService>();

    public void Dispose()
    {
        HttpClient.Dispose();
        Factory.Dispose();
    }
}

public class WebApplicationFactory<TProgram> : IDisposable
    where TProgram : class
{
    private readonly List<Action<IServiceCollection>> _configures = new();
    private readonly List<Action<IApplicationBuilder>> _appConfigures = new();
    private IServiceProvider? _serviceProvider;
    private HttpClient? _httpClient;

    public IServiceProvider Services => _serviceProvider!;

    public WebApplicationFactory<TProgram> WithWebHostBuilder(Action<Microsoft.AspNetCore.Builder.WebHostBuilder> configure)
    {
        var builder = new Microsoft.AspNetCore.Builder.WebHostBuilder();
        configure(builder);
        return this;
    }

    public HttpClient CreateClient()
    {
        _httpClient = new HttpClient();
        return _httpClient;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

public static class WebHostBuilderExtensions
{
    public static void ConfigureServices(this Microsoft.AspNetCore.Builder.WebHostBuilder builder, Action<IServiceCollection> configure)
    {
        configure(builder.Services);
    }
}

namespace Microsoft.AspNetCore.Builder
{
    public class WebHostBuilder
    {
        public IServiceCollection Services { get; } = new ServiceCollection();
    }
}