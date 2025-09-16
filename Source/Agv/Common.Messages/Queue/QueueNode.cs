namespace Gebhardt.Acc.Common.Messages.Queue;

/// <summary>
/// A queue node.
/// </summary>
/// <param name="NodeId">The node id.</param>
/// <param name="Position">The position of the node in the queue.</param>
public record QueueNode(Guid NodeId, int Position);