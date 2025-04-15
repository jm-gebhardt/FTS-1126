using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Repositories;

/// <summary>
/// Provides a repository for host order related queries.
/// </summary>
public interface IHostOrderRepository : IRepository<HostOrder>
{
    /// <summary>
    /// Update an order.
    /// </summary>
    /// <param name="hostOrder">The host order.</param>
    Task UpdateAsync(HostOrder hostOrder);

    /// <summary>
    /// Get the latest unfinished host orders by handling unit, created prior to given date.
    /// </summary>
    /// <param name="hu">The handling unit.</param>
    /// <param name="creationDate">The creation date.</param>
    /// <returns></returns>
    Task<ICollection<HostOrder>> GetLatestUnfinishedOrdersByHu(string hu, DateTime creationDate);

    /// <summary>
    /// Get the latest unfinished host order by handling unit.
    /// </summary>
    /// <param name="hu">The handling unit.</param>
    /// <returns></returns>
    Task<HostOrder?> GetLatestUnfinishedOrderByHu(string hu);

    /// <summary>
    /// Get the next drop order heading towards the Doppelspiel trigger point with the given drop target.
    /// </summary>
    /// <param name="dropTarget">The pick target to check.</param>
    Task<Guid?> GetNextDropOrderHeadingTowardsDoppelspielTriggerPoint(string dropTarget);
}