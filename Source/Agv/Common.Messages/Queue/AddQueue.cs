namespace Gebhardt.Acc.Common.Messages.Queue;

/// <summary>
/// Provides a command to add a queue.
/// </summary>
/// <param name="Name">The queue name.</param>
/// <param name="Nodes">The nodes of the queue.</param>
public record AddQueue(string Name, ICollection<QueueNode> Nodes);