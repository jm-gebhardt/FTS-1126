using System.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.Common.Persistence.Ef;

/// <summary>
/// Provides extension methods for queryables.
/// </summary>
public static class QueryableExtensions
{
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
}