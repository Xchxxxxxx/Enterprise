using System.Linq.Expressions;

namespace EfCore.Enterprise.Domain.Specifications;

public class Spec<TEntity> : ISpecification<TEntity>
{
    public Expression<Func<TEntity, bool>>? Criteria { get; private set; }
    public List<Expression<Func<TEntity, object>>> Includes { get; } = new();
    public Expression<Func<TEntity, object>>? OrderBy { get; private set; }
    public Expression<Func<TEntity, object>>? OrderByDescending { get; private set; }
    public Expression<Func<TEntity, object>>? ThenBy { get; private set; }
    public Expression<Func<TEntity, object>>? ThenByDescending { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public int? PageIndex { get; private set; }
    public int? PageSize { get; private set; }

    public static Spec<TEntity> From(Expression<Func<TEntity, bool>> criteria) => new() { Criteria = criteria };

    public Spec<TEntity> Where(Expression<Func<TEntity, bool>> criteria)
    {
        Criteria = Criteria == null ? criteria : Criteria.And(criteria);
        return this;
    }

    public Spec<TEntity> And(Spec<TEntity> other)
    {
        Criteria = Criteria == null ? other.Criteria : Criteria.And(other.Criteria!);
        return this;
    }

    public Spec<TEntity> Or(Spec<TEntity> other)
    {
        Criteria = Criteria == null ? other.Criteria : Criteria.Or(other.Criteria!);
        return this;
    }

    public Spec<TEntity> AndIf(bool condition, Expression<Func<TEntity, bool>> criteria)
    {
        if (condition) Where(criteria);
        return this;
    }

    public Spec<TEntity> Include(Expression<Func<TEntity, object>> include)
    {
        Includes.Add(include);
        return this;
    }

    public Spec<TEntity> SortBy(Expression<Func<TEntity, object>> orderBy)
    {
        OrderBy = orderBy;
        return this;
    }

    public Spec<TEntity> SortByDesc(Expression<Func<TEntity, object>> orderByDesc)
    {
        OrderByDescending = orderByDesc;
        return this;
    }

    public Spec<TEntity> ThenSortBy(Expression<Func<TEntity, object>> thenBy)
    {
        ThenBy = thenBy;
        return this;
    }

    public Spec<TEntity> ThenSortByDesc(Expression<Func<TEntity, object>> thenByDesc)
    {
        ThenByDescending = thenByDesc;
        return this;
    }

    public Spec<TEntity> Page(int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        return this;
    }

    public Spec<TEntity> Limit(int skip, int take)
    {
        Skip = skip;
        Take = take;
        return this;
    }
}

internal static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftVisitor = new ReplaceParameterVisitor(left.Parameters[0], parameter);
        var rightVisitor = new ReplaceParameterVisitor(right.Parameters[0], parameter);
        var body = Expression.AndAlso(leftVisitor.Visit(left.Body), rightVisitor.Visit(right.Body));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftVisitor = new ReplaceParameterVisitor(left.Parameters[0], parameter);
        var rightVisitor = new ReplaceParameterVisitor(right.Parameters[0], parameter);
        var body = Expression.OrElse(leftVisitor.Visit(left.Body), rightVisitor.Visit(right.Body));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    private class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            node == _oldParameter ? _newParameter : base.VisitParameter(node);
    }
}