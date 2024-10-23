using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Queue;

/// <summary>
/// A node in a queue
/// </summary>
public class QueueNode
{
    /// <summary>
    /// The id of the node.
    /// </summary>
    public string NodeId { get; set; }

    /// <summary>
    /// The position of the node in the queue.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// The corresponding <see cref="EfNode"/>.
    /// </summary>
    public EfNode Node { get; init; } = null!;

    /// <summary>
    /// The state of the node, i.e. corresponding <see cref="NodeLock"/>
    /// </summary>
    public NodeLock? State { get; init; }

    /// <inheritdoc cref="QueueNode"/>>
    public QueueNode(string nodeId, int position)
    {
        NodeId = nodeId;
        Position = position;
    }

    /// <inheritdoc cref="QueueNode"/>>
    public QueueNode(string nodeId, int position, EfNode node, NodeLock? state )
    {
        NodeId = nodeId;
        Position = position;
        Node = node;
        State = state;
    }
}