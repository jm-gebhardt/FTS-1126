using System.Linq.Expressions;
using System.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Common.Persistence.Ef;

/// <summary>
/// Provides extension methods for queryable.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Conditionally applies a filter to a queryable sequence.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The queryable sequence.</param>
    /// <param name="predicate">The filter to apply.</param>
    /// <param name="condition">Determines whether the filter should be applied.</param>
    /// <returns>The filtered or original queryable sequence based on the condition.</returns>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> source,
        Expression<Func<T, bool>> predicate,
        bool condition)
    {
        return condition ? source.Where(predicate) : source;
    }

    /// <summary>
    /// Executes the AnyAsync command in ReadUncommitted isolation scope.
    /// </summary>
    /// <typeparam name="T">The type of the queryable.</typeparam>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public static async Task<bool> AnyWithNoLockAsync<T>(this IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        using var scope = new TransactionScope(TransactionScopeOption.Required,
            new TransactionOptions()
            {
                IsolationLevel = IsolationLevel.ReadUncommitted
            },
            TransactionScopeAsyncFlowOption.Enabled);
        bool result = await query.AnyAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Executes the ToListAsync command in ReadUncommitted isolation scope.
    /// </summary>
    /// <typeparam name="T">The type of the queryable.</typeparam>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public static async Task<List<T>> ToListWithNoLockAsync<T>(this IQueryable<T> query, CancellationToken cancellationToken = default)
    {
        using var scope = new TransactionScope(TransactionScopeOption.Required,
            new TransactionOptions()
            {
                IsolationLevel = IsolationLevel.ReadUncommitted
            },
            TransactionScopeAsyncFlowOption.Enabled);
        List<T> result = await query.ToListAsync(cancellationToken);
        scope.Complete();
        return result;
    }

    /// <summary>
    /// Filters a queryable sequence by checking items against a provided string array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="query">The queryable sequence.</param>
    /// <param name="items">The string array to match against.</param>
    /// <param name="selector">Expression to select the string property for filtering.</param>
    /// <returns>The filtered queryable sequence.</returns>
    public static IQueryable<T> WhereIn<T>(
        this IQueryable<T> query,
        string[]? items,
        Expression<Func<T, string>> selector)
    {
        if (items?.Length > 0)
        {
            Expression<Func<T, bool>> predicate = Expression.Lambda<Func<T, bool>>(
                Expression.Call(
                    typeof(Enumerable),
                    nameof(Enumerable.Contains),
                    [typeof(string)],
                    Expression.Constant(items),
                    selector.Body
                ),
                selector.Parameters
            );

            query = query.Where(predicate);
        }

        return query;
    }

    /// <summary>
    /// Filters a queryable sequence based on a time range using a timestamp selector.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="query">The queryable sequence to filter.</param>
    /// <param name="fromDate">The start of the time range.</param>
    /// <param name="toDate">The end of the time range.</param>
    /// <param name="timestampSelector">The selector for the timestamp property.</param>
    /// <returns>The filtered queryable sequence.</returns>
    public static IQueryable<T> WhereInTimeRange<T>(
        this IQueryable<T> query,
        DateTime? fromDate,
        DateTime? toDate,
        Expression<Func<T, DateTime>> timestampSelector)
    {
        if (fromDate is { } fd && fd > DateTime.MinValue)
        {
            var param = timestampSelector.Parameters[0];
            var body = Expression.GreaterThanOrEqual(timestampSelector.Body, Expression.Constant(fd));
            Expression<Func<T, bool>> predicate = Expression.Lambda<Func<T, bool>>(body, param);
            query = query.Where(predicate);
        }

        if (toDate is { } td && td < DateTime.MaxValue)
        {
            var param = timestampSelector.Parameters[0];
            var body = Expression.LessThanOrEqual(timestampSelector.Body, Expression.Constant(td));
            Expression<Func<T, bool>> predicate = Expression.Lambda<Func<T, bool>>(body, param);
            query = query.Where(predicate);
        }

        return query;
    }
}