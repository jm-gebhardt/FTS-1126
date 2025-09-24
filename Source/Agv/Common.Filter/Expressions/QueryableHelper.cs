using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Exceptions;
using System.Linq.Expressions;
using System.Reflection;

namespace Gebhardt.Acc.Common.Filter.Expressions;

/// <summary>
/// Contains helper methods for working with queryables with weakly typed lambda expression.
/// </summary>
internal static class QueryableHelper
{
    private static MethodInfo GetGenericMethod(string name, Type[] generics, int? paramCount = null) => typeof(QueryableHelper).GetGenericMethod(name, generics, paramCount);

    /// <summary>
    /// Orders the queryable by the weakly typed lambda expression.
    /// </summary>
    /// <param name="queryable">The queryable to order.</param>
    /// <param name="keySelector">The key selector expression.</param>
    /// <param name="ordering"></param>
    public static IOrderedQueryable<T> OrderByAccessor<T>(this IQueryable<T> queryable, LambdaExpression keySelector, SortOrdering? ordering = SortOrdering.Desc)
    {
        var orderingMethod = ordering switch
        {
            SortOrdering.Asc => GetGenericMethod(nameof(OrderByAscending), [typeof(T), keySelector.ReturnType], 2),
            _ => GetGenericMethod(nameof(OrderByDescending), [typeof(T), keySelector.ReturnType], 2)
        };

        if (orderingMethod.Invoke(null, [queryable, keySelector]) is not IOrderedQueryable<T> result)
        {
            throw new FilterExecutionException($"Could not apply {nameof(Queryable.ThenBy)} to queryable of type {typeof(T).Name}");
        }

        return result;
    }

    private static IOrderedQueryable<T> OrderByAscending<T, TKey>(IQueryable<T> queryable, Expression<Func<T, TKey>> keySelector)
    {
        return queryable.OrderBy(keySelector);
    }

    private static IOrderedQueryable<T> OrderByDescending<T, TKey>(IQueryable<T> queryable, Expression<Func<T, TKey>> keySelector)
    {
        return queryable.OrderByDescending(keySelector);
    }

    /// <summary>
    /// Applies <see cref="Queryable.ThenBy{T,TK}(IOrderedQueryable{T}, Expression{Func{T,TK}})"/> to the queryable.
    /// </summary>
    /// <param name="queryable">The queryable to apply to.</param>
    /// <param name="expression">The key selector expression.</param>
    public static IOrderedQueryable<T> ThenOrderByAccessor<T>(this IOrderedQueryable<T> queryable, LambdaExpression expression)
    {
        var thenByMethod = GetGenericMethod(nameof(ThenOrderBy), [typeof(T), expression.ReturnType], 2);

        if (thenByMethod.Invoke(null, [queryable, expression]) is not IOrderedQueryable<T> result)
        {
            throw new FilterExecutionException($"Could not apply {nameof(Queryable.ThenBy)} to queryable of type {typeof(T).Name}");
        }

        return result;
    }

    private static IOrderedQueryable<T> ThenOrderBy<T, TKey>(IOrderedQueryable<T> queryable, Expression<Func<T, TKey>> keySelector)
    {
        return queryable.ThenBy(keySelector);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TOut"></typeparam>
    /// <typeparam name="TIn"></typeparam>
    /// <param name="queryable"></param>
    /// <param name="expression"></param>
    /// <returns></returns>
    public static IQueryable<TOut> ApplyProjection<TOut, TIn>(this IQueryable<TIn> queryable, LambdaExpression expression) where TOut : TIn
    {
        return queryable.Select(expression.AsStronglyTyped<TIn, TOut>());
    }

    /// <summary>
    /// Applies a join to a queryable.
    /// </summary>
    /// <typeparam name="TOut">The type contained in the queryable.</typeparam>
    /// <param name="queryable">The queryable to apply the join to.</param>
    /// <param name="other">The other queryable to join with.</param>
    /// <param name="keySelector">The selector expression for the key on <typeparamref name="TOut"/>.</param>
    /// <param name="otherKeySelector">The selector expression for the key on the queryable to join with.</param>
    /// <param name="combinator">Combinator expression to create a new <typeparamref name="TOut"/> from an element of the collection and other.</param>
    /// <param name="outerJoin">If this join is supposed to be a LEFT OUTER JOIN.</param>
    public static IQueryable<TOut> ApplyJoin<TOut>(this IQueryable<TOut> queryable, IQueryable other, LambdaExpression keySelector, LambdaExpression otherKeySelector, LambdaExpression combinator, bool outerJoin = false)
    {
        var otherType = other.GetType().GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryable<>)).GetGenericArguments().First();

        if (keySelector.ReturnType != otherKeySelector.ReturnType && otherKeySelector.ReturnType != Nullable.GetUnderlyingType(keySelector.ReturnType))
        {
            throw new FilterExecutionException($"Could not create join query, type mismatch in key {keySelector.ReturnType} != {otherKeySelector.ReturnType} for entities {typeof(TOut).FullName} join {otherType.FullName}");
        }

        if (typeof(TOut) != keySelector.Parameters.First().Type)
        {
            throw new FilterExecutionException($"Could not create join query, type mismatch in key selector input {typeof(TOut).FullName} != {keySelector.Parameters.First().Type.FullName} for entities {typeof(TOut).FullName} join {otherType.FullName}");
        }

        if (otherType != otherKeySelector.Parameters.First().Type)
        {
            throw new FilterExecutionException($"Could not create join query, type mismatch in other key selector input {otherType.FullName} != {otherKeySelector.Parameters.First().Type.FullName} for entities {typeof(TOut).FullName} join {otherType.FullName}");
        }

        var keyType = keySelector.ReturnType;

        return outerJoin ? ApplyOuterJoin(queryable, other, keySelector, otherKeySelector, combinator, otherType, keyType) : ApplyInnerJoin(queryable, other, keySelector, otherKeySelector, combinator, otherType, keyType);
    }

    private static IQueryable<TOut> ApplyOuterJoin<TOut>(IQueryable<TOut> queryable, IQueryable other, LambdaExpression keySelector, LambdaExpression otherKeySelector, LambdaExpression combinator, Type otherType, Type keyType)
    {
        var method = GetGenericMethod(nameof(LeftOuterJoin), [typeof(TOut), otherType, keyType, typeof(TOut)], 5);

        if (method.Invoke(null, [queryable, other, keySelector, otherKeySelector, combinator]) is not IQueryable<TOut> result)
        {
            throw new FilterExecutionException($"Could not create left outer join query for entities {typeof(TOut).FullName} join {otherType.FullName}");
        }
        return result;
    }

    private static IQueryable<TOut> ApplyInnerJoin<TOut>(IQueryable<TOut> queryable, IQueryable other, LambdaExpression keySelector, LambdaExpression otherKeySelector, LambdaExpression combinator, Type otherType, Type keyType)
    {
        var joinMethod = GetGenericMethod(nameof(InnerJoin), [typeof(TOut), otherType, keyType, typeof(TOut)], 5);

        if (joinMethod.Invoke(null, [queryable, other, keySelector, otherKeySelector, combinator]) is not IQueryable<TOut> result)
        {
            throw new FilterExecutionException($"Could not create join query for entities {typeof(TOut).FullName} join {otherType.FullName}");
        }

        return result;
    }

    private static IQueryable<TResult> InnerJoin<TOuter, TInner, TKey, TResult>(IQueryable<TOuter> outerSequence, IEnumerable<TInner> innerSequence, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
    {
        return outerSequence.Join(innerSequence, outerKeySelector, innerKeySelector, resultSelector);
    }

    private static IQueryable<TResult> LeftOuterJoin<TOuter, TInner, TKey, TResult>(IQueryable<TOuter> outerSequence, IEnumerable<TInner> innerSequence, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner?, TResult>> resultSelector)
    {
        return outerSequence.GroupJoin(
            innerSequence,
            outerKeySelector,
            innerKeySelector,
            (outer, inner) =>
                new
                {
                    Outer = outer,
                    InnerCollection = inner
                }
        ).SelectMany(
            other => other.InnerCollection.DefaultIfEmpty(),
            // ReSharper disable once RedundantAnonymousTypePropertyName used in SelectOuterInner
            (self, other) => new { Outer = self.Outer, Inner = other }
        ).SelectOuterInner(resultSelector);
    }

    private static IQueryable<TResult> SelectOuterInner<TOuter, TInner, T, TResult>(this IQueryable<T> queryable, Expression<Func<TOuter, TInner?, TResult>> resultSelector)
    {
        var param = Expression.Parameter(typeof(T), "anonymousTypeParam");
        var outer = Expression.PropertyOrField(param, "Outer");
        var inner = Expression.PropertyOrField(param, "Inner");

        var resultBody = resultSelector.Body.ReplaceParameter(resultSelector.Parameters[0], outer);
        resultBody = resultBody.ReplaceParameter(resultSelector.Parameters[1], inner);

        Expression<Func<T, TResult>> lambda = Expression.Lambda<Func<T, TResult>>(resultBody, false, param);

        return queryable.Select(lambda);
    }
}