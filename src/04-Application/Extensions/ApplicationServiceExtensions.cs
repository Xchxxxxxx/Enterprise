using EfCore.Enterprise.Shared.DependencyInjection;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EfCore.Enterprise.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddInjectables(assembly);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assembly);
            cfg.Lifetime = ServiceLifetime.Scoped;
        });

        services.AddAutoMapper(assembly);

        services.AddValidatorsFromAssemblies(new[] { assembly });

        return services;
    }
}