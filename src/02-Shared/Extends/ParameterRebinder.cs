using System.Linq.Expressions;

namespace EfCore.Enterprise.Shared.Extends;

public class ParameterRebinder : ExpressionVisitor
{
    private readonly ParameterExpression _oldParam;
    private readonly ParameterExpression _newParam;

    public ParameterRebinder(ParameterExpression oldParam, ParameterExpression newParam)
    {
        _oldParam = oldParam;
        _newParam = newParam;
    }

    protected override Expression VisitParameter(ParameterExpression p)
    {
        return p == _oldParam ? _newParam : base.VisitParameter(p);
    }
}