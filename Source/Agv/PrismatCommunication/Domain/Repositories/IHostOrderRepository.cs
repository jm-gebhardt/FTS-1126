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
    ///Check if there are host orders that are heading towards the decision point to get a drop target assigned.
    /// </summary>
    Task<bool> HasDropOrdersHeadingTowardsDecisionPoint();

    /// <summary>
    ///Check if there drop host orders that are heading towards the check cargo trigger point.
    /// </summary>
    /// <returns></returns>
    Task<bool> HasDropOrdersHeadingTowardsCargoTriggerPoint();

    /// <summary>
    ///Check if there drop host orders that are heading towards the short loop trigger point.
    /// </summary>
    /// <returns></returns>
    Task<bool> HasDropOrdersHeadingTowardsShortLoopTriggerPoint();

    /// <summary>
    /// Get the next order with unknown pick target.
    /// </summary>
    Task<HostOrder?> GetNextIncompleteOrderWithUnknownPickTarget();

    /// <summary>
    /// Get the next order with unknown drop target.
    /// </summary>
    Task<HostOrder?> GetNextIncompleteOrderWithUnknownDropTarget();

    /// <summary>
    /// Gets the next order picking at 1020 for dual operation.
    /// </summary>
    Task<HostOrder?> GetNextPickOrderForShortLoopDualOperation();

    /// <summary>
    /// Get a host order by handling unit.
    /// </summary>
    /// <param name="hu">The handling unit.</param>
    /// <returns></returns>
    Task<HostOrder?> GetByHu(string hu);

    /// <summary>
    /// Get the next host order picking at one of the pick targets that has not been queued.
    /// </summary>
    /// <param name="pickTarget">The pick target to check.</param>
    /// <param name="unknownDrop">If the drop is known</param>
    Task<HostOrder?> GetNextNotQueuedHostOrderPickingAt(string? pickTarget, bool unknownDrop = false);
}