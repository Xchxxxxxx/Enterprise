using System.Linq.Expressions;

namespace EfCore.Enterprise.Domain.Specifications;

public interface ISpecification<TEntity>
{
    Expression<Func<TEntity, bool>>? Criteria { get; }
    List<Expression<Func<TEntity, object>>> Includes { get; }
    Expression<Func<TEntity, object>>? OrderBy { get; }
    Expression<Func<TEntity, object>>? OrderByDescending { get; }
    Expression<Func<TEntity, object>>? ThenBy { get; }
    Expression<Func<TEntity, object>>? ThenByDescending { get; }
    int? Skip { get; }
    int? Take { get; }
    int? PageIndex { get; }
    int? PageSize { get; }
}