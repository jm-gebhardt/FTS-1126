using JetBrains.Annotations;

namespace Gebhardt.Acc.Provisioning.Models;

/// <summary>
/// A queue
/// </summary>
[PublicAPI]
public class Queue
{
    /// <summary>
    /// The name of the queue.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// The nodes of the queue.
    /// </summary>
    public ICollection<QueueNode> Nodes { get; init; }

    /// <inheritdoc cref="Queue"/>>
    public Queue(string name, ICollection<QueueNode> nodes)
    {
        Name=name;
        Nodes=nodes;
    }
}

/// <summary>
/// A node in the queue.
/// </summary>
[PublicAPI]
public class QueueNode
{
    /// <summary>
    /// The node id.
    /// </summary>
    public string NodeId { get; init; }

    /// <summary>
    /// The position of the node in the queue.
    /// </summary>
    public int Position { get; init; }

    /// <inheritdoc cref="QueueNode"/>>
    public QueueNode(string nodeId, int position)
    {
        NodeId=nodeId;
        Position=position;
    }
}