using System.Linq.Expressions;
using System.Reflection;
using Gebhardt.StoreWare.Agv.Common.Exceptions;

namespace Gebhardt.StoreWare.Agv.Common.Extensions;

/// <summary>
/// Provides extension methods for sorting a queryable.
/// </summary>
public static  class QueryableExtension
{
    private static readonly MethodInfo _orderByMethod =
        typeof(Queryable).GetMethods().Single(method =>
            method.Name == "OrderBy" && method.GetParameters().Length == 2);

    private static readonly MethodInfo _orderByDescendingMethod =
        typeof(Queryable).GetMethods().Single(method =>
            method.Name == "OrderByDescending" && method.GetParameters().Length == 2);

    /// <summary>
    /// Order the queryable by given property.
    /// </summary>
    /// <typeparam name="T">The queryable type.</typeparam>
    /// <param name="queryable">The queryable.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="isDescending">Flag, if true sorts by descending order.</param>
    public static IOrderedQueryable<T> OrderByProperty<T>(
        this IQueryable<T> queryable, string propertyName, bool isDescending=true)
    {
        if (typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase |
                                                BindingFlags.Public | BindingFlags.Instance) == null)
        {
            throw new PropertyNotFoundException(propertyName);
        }
        var parameterExpression = Expression.Parameter(typeof(T));
        Expression orderByProperty = Expression.Property(parameterExpression, propertyName);
        var lambda = Expression.Lambda(orderByProperty, parameterExpression);
        var sortingMethod = _orderByMethod.MakeGenericMethod(typeof(T), orderByProperty.Type);
        if (!isDescending)
        {
            return (IOrderedQueryable<T>)sortingMethod.Invoke(null, new object[] { queryable, lambda })!;
        }

        sortingMethod = _orderByDescendingMethod.MakeGenericMethod(typeof(T), orderByProperty.Type);
        return (IOrderedQueryable<T>)sortingMethod.Invoke(null, new object[] { queryable, lambda })!;
    }
}