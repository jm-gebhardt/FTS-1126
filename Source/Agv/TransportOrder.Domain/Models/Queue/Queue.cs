using System.Text.Json.Serialization;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Queue;

/// <summary>
/// Represents a queue
/// </summary>
public class Queue
{
    /// <summary>
    /// The nodes corresponding to the queue.
    /// </summary>
    public ICollection<QueueNode> Nodes { get; init; } = [];

    /// <summary>
    /// The name of the queue.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The id of the queue.
    /// </summary>
    public Guid Id { get; init; }

    /// <inheritdoc cref="Queue"/>>
    public Queue(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    /// <inheritdoc cref="Queue"/>>
    public Queue(Guid id, string name, ICollection<QueueNode> nodes)
    {
        Id = id;
        Name = name;
        Nodes = nodes;
    }

    /// <inheritdoc cref="Queue"/>>
    [JsonConstructor]
    public Queue()
    {
    }

    /// <summary>
    /// Add a node to a queue.
    /// </summary>
    /// <param name="nodeId">The node id.</param>
    /// <param name="position">The position of the node in the queue.</param>
    public void AddNode(string nodeId, int position)
    {
        if (Nodes.Any(n => n.NodeId == nodeId))
        {
            throw new QueueNodeExistsException(nodeId);
        }
        if (Nodes.Any(n => n.Position == position))
        {
            throw new QueueNodeExistsException(position);
        }
        Nodes.Add(new QueueNode(nodeId, position));
    }

    /// <summary>
    /// Remove a node from the queue.
    /// </summary>
    /// <param name="nodeId">The id of the node.</param>
    /// <exception cref="NodeNotFoundException">Thrown, if no node with the given id was found in the queue.</exception>
    public void RemoveNode(string nodeId)
    {
        var node = Nodes.SingleOrDefault(n => n.NodeId == nodeId);
        if (node == null)
        {
            throw new NodeNotFoundException(nodeId);
        }
        Nodes.Remove(node);
    }
}