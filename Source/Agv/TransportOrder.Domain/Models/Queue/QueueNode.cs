using System.Text.Json.Serialization;
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
    public string NodeId { get; init; } = string.Empty;

    /// <summary>
    /// The position of the node in the queue.
    /// </summary>
    public int Position { get; init; }

    /// <summary>
    /// The corresponding <see cref="EfNode"/>.
    /// </summary>
    public EfNode? Node { get; set; } 

    /// <summary>
    /// The state of the node, i.e. corresponding <see cref="NodeLock"/>
    /// </summary>
    public NodeLock? State { get; set; }

    /// <inheritdoc cref="QueueNode"/>>
    [JsonConstructor]
    public QueueNode()
    {
    }

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

    /// <summary>
    /// Reserve a queue node for an agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv to reserve the queue node for.</param>
    public void Reserve(Guid agvCorrelationId)
    {
        State = new NodeLock(NodeId, NodeLockState.Reserved, false, agvCorrelationId, DateTime.UtcNow);
    }
}