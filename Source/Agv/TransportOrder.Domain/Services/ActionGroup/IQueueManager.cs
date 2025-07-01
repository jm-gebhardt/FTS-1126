using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;

/// <summary>
/// Manages node queues
/// </summary>
public interface IQueueManager
{
    /// <summary>
    /// Try to enqueue the agv into the closest queue.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="entryNode">The entry node of the queue.</param>
    /// <returns>The node where to enqueue the agv, if any.</returns>
    Task<string?> TryEnqueueAgv(Guid agvCorrelationId, string entryNode);

    /// <summary>
    /// Try to enter a queue.
    /// </summary>
    /// <param name="agv">The agv that enters a queue.</param>
    /// <returns>The entry node of the queue.</returns>
    Task<string?> TryEnterQueue(Agv agv);

    /// <summary>
    /// Try to dequeue an agv.
    /// </summary>
    /// <returns>Object containing the agvs that need to move to the next queue node.</returns>
    Task<Dictionary<Guid, string>?> TryDequeueAgv(Guid agvCorrelationId);

    /// <summary>
    /// Try to escape the queue.
    /// </summary>
    /// <returns>Object containing the agvs that need to move to the next queue node.</returns>
    Task<(Dictionary<Guid, string>? agvsToMove, int? queueSize)> TryEscapeQueue(Guid agvCorrelationId, string? agvLastNodeId);

    /// <summary>
    /// True, if the agv is on a queue node.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    Task<bool> IsOnAQueueNode(Guid agvCorrelationId);

    /// <summary>
    /// Try to reach the queue node.
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <param name="nodeId">The node id.</param>
    /// <returns>True, if the agv is now at queue head.</returns>
    Task<bool> TryReachQueueNode(Agv agv, string nodeId);

    /// <summary>
    /// Get all agvs at queue heads.
    /// </summary>
    /// <returns>The agvs at queue heads.</returns>
    Task<ICollection<Guid>> GetAgvsAtQueueHeads();

    /// <summary>
    /// Remove an agv from its queue.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="targetNode">The target node of the queue.</param>
    /// <returns>True, if the agv was removed from the queue.</returns>
    Task<bool> RemoveAgvFromQueueEntry(Guid agvCorrelationId, string targetNode);

    /// <summary>
    /// Gets all the action groups that are internal to the queue that the agv might be in.
    /// </summary>
    /// <param name="agv">The agv to check.</param>
    /// <returns>A collection of action group ids.</returns>
    Task<ICollection<Guid>> GetQueueInternalActionGroups(Agv agv);
}