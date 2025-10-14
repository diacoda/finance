using System.Linq.Expressions;

namespace Finance.Tracking.Extensions;
// Expression extension to combine predicates dynamically
public static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> AndAlso<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var param = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(expr1, param),
            Expression.Invoke(expr2, param)
        );
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}