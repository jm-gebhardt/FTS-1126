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
    /// Get all queues
    /// </summary>
    /// <returns>A collection of queues</returns>
    Task<ICollection<Queue>> GetQueues();

    /// <summary>
    /// Get a single queue by its id.
    /// </summary>
    /// <param name="id">The id of the queue.</param>
    /// <returns>The queue.</returns>
    /// <exception cref="QueueNotFoundException">When no queue with the given id exists.</exception>
    Task<Queue> GetQueueById(Guid id);

    /// <summary>
    /// Remove a queue.
    /// </summary>
    /// <param name="id">The id of the queue.</param>
    /// <exception cref="QueueNotFoundException">When no queue with the given id exists.</exception>
    Task RemoveQueue(Guid id);
}