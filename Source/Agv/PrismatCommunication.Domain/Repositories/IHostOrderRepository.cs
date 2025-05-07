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

    /// <summary>
    /// Get the next host order to be queued for a station group given by sources and targets.
    /// </summary>
    /// <param name="sources">The sources of the station group.</param>
    /// <param name="targets">The targets of the station group.</param>
    /// <returns>A host order, if any is to be queued.</returns>
    Task<HostOrder?> GetNextHostOrderForStationGroup(ICollection<string> sources, ICollection<string> targets);

    /// <summary>
    /// Check if there are currently running host orders for a station group given by sources and targets.
    /// </summary>
    /// <param name="sources">The sources of the station group.</param>
    /// <param name="targets">The targets of the station group.</param>
    /// <returns>True, if there are running host orders to this station group.</returns>
    Task<bool> HasCurrentHostOrdersForStationGroup(ICollection<string> sources, ICollection<string> targets);
}