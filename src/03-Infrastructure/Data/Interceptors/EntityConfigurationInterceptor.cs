using System.Reflection;
using EfCore.Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EfCore.Enterprise.Infrastructure.Data.Interceptors;

public class EntityConfigurationScanInterceptor
{
    public static void AutoConfigureEntities(
        ModelBuilder modelBuilder,
        Assembly assembly)
    {
        var entityTypes = assembly.GetTypes()
            .Where(t =>
                !t.IsAbstract &&
                !t.IsInterface &&
                t.Namespace != null &&
                t.Namespace.Contains(".Entities") &&
                typeof(BaseEntity<long>).IsAssignableFrom(t))
            .ToList();

        foreach (var entityType in entityTypes)
        {
            modelBuilder.Entity(entityType);
        }
    }
}