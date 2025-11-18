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
    /// Extension block for <see cref="IQueryable{T}"/>.
    /// </summary>
    extension<T>(IQueryable<T> source)
    {
        /// <summary>
        /// Conditionally applies a filter to a queryable sequence.
        /// </summary>
        /// <param name="predicate">The filter to apply.</param>
        /// <param name="condition">Determines whether the filter should be applied.</param>
        /// <returns>The filtered or original queryable sequence based on the condition.</returns>
        public IQueryable<T> WhereIf(
            Expression<Func<T, bool>> predicate,
            bool condition)
        {
            return condition ? source.Where(predicate) : source;
        }

        /// <summary>
        /// Executes the AnyAsync command in ReadUncommitted isolation scope.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<bool> AnyWithNoLockAsync(CancellationToken cancellationToken = default)
        {
            using var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions()
                {
                    IsolationLevel = IsolationLevel.ReadUncommitted
                },
                TransactionScopeAsyncFlowOption.Enabled);
            bool result = await source.AnyAsync(cancellationToken);
            scope.Complete();
            return result;
        }

        /// <summary>
        /// Executes the ToListAsync command in ReadUncommitted isolation scope.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<List<T>> ToListWithNoLockAsync(CancellationToken cancellationToken = default)
        {
            using var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions()
                {
                    IsolationLevel = IsolationLevel.ReadUncommitted
                },
                TransactionScopeAsyncFlowOption.Enabled);
            List<T> result = await source.ToListAsync(cancellationToken);
            scope.Complete();
            return result;
        }

        /// <summary>
        /// Filters a queryable sequence by checking items against a provided string array.
        /// </summary>
        /// <param name="items">The string array to match against.</param>
        /// <param name="selector">Expression to select the string property for filtering.</param>
        /// <returns>The filtered queryable sequence.</returns>
        public IQueryable<T> WhereIn(
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

                return source.Where(predicate);
            }

            return source;
        }

        /// <summary>
        /// Filters a queryable sequence based on a time range using a timestamp selector.
        /// </summary>
        /// <param name="fromDate">The start of the time range.</param>
        /// <param name="toDate">The end of the time range.</param>
        /// <param name="timestampSelector">The selector for the timestamp property.</param>
        /// <returns>The filtered queryable sequence.</returns>
        public IQueryable<T> WhereInTimeRange(
            DateTime? fromDate,
            DateTime? toDate,
            Expression<Func<T, DateTime>> timestampSelector)
        {
            IQueryable<T> query = source;

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
}
