using System.Linq.Expressions;

namespace EfCore.Enterprise.Shared.Extends;

public static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> AndAlso<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var rebinder = new ParameterRebinder(right.Parameters[0], left.Parameters[0]);
        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(left.Body, rebinder.Visit(right.Body)),
            left.Parameters);
    }

    public static Expression<Func<T, bool>> OrElse<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var rebinder = new ParameterRebinder(right.Parameters[0], left.Parameters[0]);
        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(left.Body, rebinder.Visit(right.Body)),
            left.Parameters);
    }
}