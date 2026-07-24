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
    /// <param name="cancellationToken">The cancellation token</param>
    Task AddQueue(Queue queue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update a queue.
    /// </summary>
    /// <param name="queue">The queue</param>
    /// <param name="cancellationToken">The cancellation token</param>
    Task<bool> UpdateQueueAsync(Queue queue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the queue size.
    /// </summary>
    /// <param name="queueId">The queue id</param>
    /// <param name="newSize">The new size</param>
    /// <param name="cancellationToken">The cancellation token</param>
    Task UpdateQueueSize(Guid queueId, int newSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all queues
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A collection of queues</returns>
    Task<ICollection<Queue>> GetQueues(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the id of queue nodes that are blocked by agvs.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Collection of node ids.</returns>
    Task<ICollection<Guid>> GetBlockedQueueNodes(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single queue by its id.
    /// </summary>
    /// <param name="id">The id of the queue.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The queue if found.</returns>
    Task<Queue?> GetQueueByIdIfExisting(Guid? id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single queue by its id.
    /// </summary>
    /// <param name="id">The id of the queue.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The queue.</returns>
    /// <exception cref="QueueNotFoundException">When no queue with the given id exists.</exception>
    Task<Queue> GetQueueById(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a single queue by its name.
    /// </summary>
    /// <param name="name">The name of the queue.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The queue.</returns>
    /// <exception cref="QueueNotFoundException">When no queue with the given id exists.</exception>
    Task<Queue> GetQueueByName(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a queue.
    /// </summary>
    /// <param name="id">The id of the queue.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <exception cref="QueueNotFoundException">When no queue with the given id exists.</exception>
    Task RemoveQueue(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all queues that are not full.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>All queues that are not full.</returns>
    Task<ICollection<Queue>> GetQueuesWithCapacity(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the queue that has the given entry node.
    /// </summary>
    /// <param name="entryNode">The id of the entry node.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The (first) queue that has the given entry node.</returns>
    Task<Queue?> GetQueueByEntryNode(Guid entryNode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all queues that are occupied by agvs.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Collection of queues.</returns>
    Task<ICollection<Queue>> GetOccupiedQueues(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the queue an agv is in.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Queue, if agv is in any.</returns>
    Task<Queue?> GetQueueForAgv(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the queue by one of its queue nodes.
    /// </summary>
    /// <param name="queueNodeId">The Id of the queue node.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The queue, if existing.</returns>
    Task<Queue?> GetQueueByQueueNodeId(Guid queueNodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all queues on the route given by its nodes.
    /// </summary>
    /// <param name="nodes">The route nodes.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Collection of queues.</returns>
    Task<ICollection<Queue>> GetQueuesOnRoute(ICollection<Guid> nodes, CancellationToken cancellationToken = default);
}