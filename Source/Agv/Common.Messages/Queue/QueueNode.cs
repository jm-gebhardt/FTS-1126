using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.Common.Messages.Queue;

/// <summary>
/// A queue node.
/// </summary>
/// <param name="Node">The node.</param>
/// <param name="Position">The position of the node in the queue.</param>
/// <param name="ReservedAgv">The agv the node is reserved for.</param>
/// <param name="OccupiedAgv">The agv that occupies the queue node.</param>
public record QueueNode(BaseNode Node, int Position, Guid? ReservedAgv, Guid? OccupiedAgv);