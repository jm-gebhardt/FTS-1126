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
    /// Get the next drop order that could run a Doppelspiel.
    /// </summary>
    /// <param name="dropTargets">The target to check.</param>
    Task<Guid?> GetNextDropOrderEligibleForDoppelspiel(string[] dropTargets);

    /// <summary>
    /// Get the next host order to be queued for a station group given by sources and targets.
    /// </summary>
    /// <param name="sources">The sources of the station group.</param>
    /// <param name="targets">The targets of the station group.</param>
    /// <returns>A host order, if any is to be queued.</returns>
    Task<HostOrder?> GetNextHostOrderForStationGroup(ICollection<string> sources, ICollection<string> targets);

    /// <summary>
    /// Check if there are any queued or currently running host orders for a station group given by sources and targets.
    /// </summary>
    /// <param name="sources">The sources of the station group.</param>
    /// <param name="targets">The targets of the station group.</param>
    /// <param name="agvCorrelationId">The correlation id of the AGV that is excluded from the check.</param>
    /// <returns>True, if there are host orders (of a different or unassigned Agv) to this station group already queued or running. This includes not running, but queued orders.</returns>
    Task<bool> HasHostOrdersForStationGroup(ICollection<string> sources, ICollection<string> targets, Guid? agvCorrelationId);

    /// <summary>
    /// Get the next order for the given source SAPs.
    /// </summary>
    /// <param name="sources">The source SAPs.</param>
    /// <returns>A host order, if there is any.</returns>
    Task<HostOrder?> GetNextPickForSources(ICollection<string> sources);

    /// <summary>
    /// Get the next pick order that was registered for Doppelspiel, but has no matching partner.
    /// </summary>
    /// <param name="sources">The source SAPs.</param>
    /// <param name="targets">The target SAPs.</param>
    /// <param name="correlationId">The current drop order correlation id.</param>
    /// <returns>A host order, if there is any.</returns>
    Task<HostOrder?> GetNextUnmatchedDoppelspielPick(ICollection<string> sources, ICollection<string> targets, Guid? correlationId);

    /// <summary>
    /// Get the next drop order that was registered for Doppelspiel, but has no matching partner.
    /// </summary>
    /// <param name="sources">The source SAPs.</param>
    /// <param name="targets">The target SAPs.</param>
    /// <param name="correlationId">The current drop order correlation id.</param>
    /// <returns>A host order, if there is any.</returns>
    Task<HostOrder?> GetNextUnmatchedDoppelspielDrop(ICollection<string> sources, ICollection<string> targets, Guid? correlationId);

    /// <summary>
    /// Gets the current host order for an agv.
    /// </summary>
    /// <param name="agv">The correlation id of the agv.</param>
    Task<HostOrder?> GetCurrentHostOrderForAgv(Guid agv);
}