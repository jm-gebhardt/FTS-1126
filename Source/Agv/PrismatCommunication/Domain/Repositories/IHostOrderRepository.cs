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
    ///Check if there drop host orders that are heading towards the short circle trigger point.
    /// </summary>
    /// <returns></returns>
    Task<bool> HasDropOrdersHeadingTowardsShortCircleTriggerPoint();

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
    Task<HostOrder?> GetNextPickOrderForShortCircleDualOperation();
}