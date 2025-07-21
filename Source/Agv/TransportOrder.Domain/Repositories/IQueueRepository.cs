using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for <see cref="Queue"/> related queries
/// </summary>
public interface IQueueRepository
{
    /// <summary>
    /// Add a queue.
    /// </summary>
    /// <param name="queue">The queue</param>
    Task AddQueue(Queue queue);

    /// <summary>
    /// Update a queue.
    /// </summary>
    /// <param name="queue">The queue</param>
    Task<bool> UpdateQueueAsync(Queue queue);

    /// <summary>
    /// Update the queue size.
    /// </summary>
    /// <param name="queueId">The queue id</param>
    /// <param name="newSize">The new size</param>
    /// <returns></returns>
    Task UpdateQueueSize(Guid queueId, int newSize);

    /// <summary>
    /// Get all queues
    /// </summary>
    /// <returns>A collection of queues</returns>
    Task<ICollection<Queue>> GetQueues();

    /// <summary>
    /// Get the id of queue nodes that are blocked by agvs.
    /// </summary>
    /// <returns>Collection of node ids.</returns>
    Task<ICollection<string>> GetBlockedQueueNodes();

    /// <summary>
    /// Get a single queue by its id.
    /// </summary>
    /// <param name="id">The id of the queue.</param>
    /// <returns>The queue.</returns>
    /// <exception cref="QueueNotFoundException">When no queue with the given id exists.</exception>
    Task<Queue> GetQueueById(Guid id);

    /// <summary>
    /// Get a single queue by its name.
    /// </summary>
    /// <param name="name">The name of the queue.</param>
    /// <returns>The queue.</returns>
    /// <exception cref="QueueNotFoundException">When no queue with the given id exists.</exception>
    Task<Queue> GetQueueByName(string name);

    /// <summary>
    /// Remove a queue.
    /// </summary>
    /// <param name="id">The id of the queue.</param>
    /// <exception cref="QueueNotFoundException">When no queue with the given id exists.</exception>
    Task RemoveQueue(Guid id);

    /// <summary>
    /// Get all queues that are not full.
    /// </summary>
    /// <returns>All queues that are not full.</returns>
    Task<ICollection<Queue>> GetQueuesWithCapacity();

    /// <summary>
    /// Get the queue that has the given entry node.
    /// </summary>
    /// <param name="entryNode">The id of the entry node.</param>
    /// <returns>The (first) queue that has the given entry node.</returns>
    Task<Queue?> GetQueueByEntryNode(string entryNode);

    /// <summary>
    /// Get all queues that are occupied by agvs.
    /// </summary>
    /// <returns></returns>
    Task<ICollection<Queue>> GetOccupiedQueues();

    /// <summary>
    /// Get the queue an agv is in.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <returns>Queue, if agv is in any.</returns>
    Task<Queue?> GetQueueForAgv(Guid agvCorrelationId);
}