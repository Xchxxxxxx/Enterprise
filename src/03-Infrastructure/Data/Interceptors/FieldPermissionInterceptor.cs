using EfCore.Enterprise.Shared.DependencyInjection;
using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCore.Enterprise.Infrastructure.Data.Interceptors;

[Injectable(ServiceLifetime.Singleton)]
public class FieldPermissionInterceptor : IQueryExpressionInterceptor
{
    public Expression QueryCompilationStarting(
        Expression queryExpression,
        QueryExpressionEventData eventData)
    {
        return queryExpression;
    }
}