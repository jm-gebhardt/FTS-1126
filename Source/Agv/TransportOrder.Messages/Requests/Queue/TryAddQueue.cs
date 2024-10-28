using Gebhardt.Acc.Common.Messages.Queue;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;

/// <summary>
/// Provides a quest to add a queue.
/// </summary>
/// <param name="Name">The queue name.</param>
/// <param name="Nodes">The nodes of a queue.</param>
public record TryAddQueue(string Name, ICollection<QueueNode> Nodes);