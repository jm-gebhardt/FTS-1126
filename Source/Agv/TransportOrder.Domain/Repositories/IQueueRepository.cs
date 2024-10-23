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
}