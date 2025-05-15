using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    ///  The node at the position from which to dequeue if the queue isn't empty.
    /// </summary>
    public QueueNode? Head
    {
        get
        {
            var node = Nodes.MinBy(n => n.Position);
            bool isOccupied = node?.OccupiedAgv != null ;
            if (isOccupied)
            {
                return node;
            }
            return null;
        }
    }

    /// <summary>
    /// The node at the position at which to enqueue if the queue isn't full.
    /// </summary>
    public QueueNode? Tail => Nodes.OrderBy(n => n.Position).FirstOrDefault(n => n.GetCurrentAgv() == null);

    /// <summary>
    /// The id of the node where agvs enter the queue.
    /// </summary>
    public string? EntryNode => Nodes.OrderBy(n => n.Position).LastOrDefault()?.NodeId;

    /// <summary>
    /// The agv fill quantity.
    /// </summary>
    public int AgvFillQuantity { get; set; }

    /// <summary>
    /// True, if the queue is full.
    /// </summary>
    public bool IsFull => AgvFillQuantity == Nodes.Count;

    /// <summary>
    /// True if all agvs reached the queue.
    /// </summary>
    public bool AllAgvsEntered => Nodes.Count(n => n.OccupiedAgv != null || n.ReservedAgv != null) == Nodes.Count;

    /// <summary>
    /// The agvs in the queue, ordered from entry node to head.
    /// </summary>
    public List<Guid> Agvs => Nodes.OrderByDescending(n => n.Position).Where(n => n.GetCurrentAgv() != null).Select(n => n.GetCurrentAgv()!.Value).ToList();
    
    /// <inheritdoc cref="Queue"/>>
    public Queue(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    /// <inheritdoc cref="Queue"/>>
    public Queue(Guid id, string name, ICollection<QueueNode> nodes, int agvFillQuantity)
    {
        Id = id;
        Name = name;
        Nodes = nodes;
        AgvFillQuantity = agvFillQuantity;
    }

    /// <inheritdoc cref="Queue"/>>
    [JsonConstructor]
    public Queue()
    {
    }

    /// <summary>
    /// Try to enter a queue.
    /// </summary>
    /// <returns>Id of entry node.</returns>
    public string? TryEnterQueue()
    {
        if (IsFull)
        {
            return null;
        }
        AgvFillQuantity += 1;
        return EntryNode;
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

    /// <summary>
    /// Enqueue an agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv to enqueue.</param>
    /// <param name="targetNode">The id of the target node to enqueue to.</param>
    public bool TryEnqueue(Guid agvCorrelationId, [MaybeNullWhen(false)] out string targetNode)
    {
        if (AgvFillQuantity==0 )
        { 
            targetNode = null;
            return false;
        }
        if (AllAgvsEntered && !IsEnqueued(agvCorrelationId))
        {
            targetNode = EntryNode!;
            return true;
        }
        int tail = Tail!.Position ;
        var node = Nodes.Single(n => n.Position == tail);
        if (node.NodeId == EntryNode)
        {
             node.Occupy(agvCorrelationId);
             targetNode = node.NodeId;
             return true;
        }
        node.Reserve(agvCorrelationId);
        targetNode = node.NodeId;
        return true;
    }

    /// <summary>
    /// Dequeue an agv.
    /// </summary>
    /// <param name="agvsToMove">Output: Agvs that are to move down the queue and their target node ids.</param>
    public Guid? TryDequeue(out Dictionary<Guid, string> agvsToMove)
    {
        agvsToMove = new Dictionary<Guid, string>();
        if (AgvFillQuantity == 0 || Head == null)
        {
            return null;
        }
        int head = Head!.Position;
        var node = Nodes.Single(n => n.Position == head);
        if (node.OccupiedAgv == null)
        {
            return null;
        }
        var agv = node.OccupiedAgv!.Value;
        ShiftDown(ref agvsToMove, head);
        node.RemoveAgv(agv);
        AgvFillQuantity -= 1;
        AgvFillQuantity = Math.Max(AgvFillQuantity, 0);
        return agv;
    }

    /// <summary>
    /// Shift agvs down in the queue.
    /// </summary>
    /// <param name="agvsToMove">Output: agvs to move one position down.</param>
    /// <param name="startPosition">The position from which to start moving down.</param>
    private void ShiftDown(ref Dictionary<Guid, string> agvsToMove, int startPosition)
    {
        foreach (var node in Nodes.Where(n => n.Position > startPosition && n.GetCurrentAgv() != null).OrderBy(n => n.Position))
        {
            Guid? agv = node.GetCurrentAgv();
            if (agv == null)
            {
                continue;
            }
            node.RemoveAgv(agv.Value);
            var target = Nodes.Single(n => n.Position == node.Position - 1);
            target.Reserve(agv.Value);
            agvsToMove.Add(agv.Value, target.NodeId);
        }
    }

    /// <summary>
    /// True, if an agv has already been enqueued.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    public bool IsEnqueued(Guid agvCorrelationId)
    {
        return Nodes.Any(n => n.AgvIsAssigned(agvCorrelationId));
    }

    /// <summary>
    /// True, if the last node is occupied by or reserved for the given
    /// agv and is the given node.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the node.</param>
    /// <param name="nodeId">The id of the node.</param>
    /// <returns></returns>
    public bool IsAgvAtQueueHead(Guid agvCorrelationId, string nodeId)
    {
        var lastNode = Nodes.MinBy(n => n.Position);
        return lastNode!= null && lastNode.AgvIsAssigned(agvCorrelationId) && lastNode.NodeId == nodeId;
    }

    /// <summary>
    /// Escape the queue.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv that escapes.</param>
    /// <param name="agvsToMove">The Output: agvs to move one position down</param>
    /// <returns>Node at which the queue was escaped.</returns>
    public string? TryEscapeQueue(Guid agvCorrelationId, out Dictionary<Guid, string> agvsToMove)
    {
        agvsToMove = new Dictionary<Guid, string>();
        var node = Nodes.SingleOrDefault(n => n.OccupiedAgv == agvCorrelationId);
        if (node == null)
        {
            return null;
        }
        ShiftDown(ref agvsToMove, node.Position);
        node.RemoveAgv(agvCorrelationId);
        AgvFillQuantity -= 1;
        AgvFillQuantity = Math.Max(AgvFillQuantity, 0);
        return node.NodeId;
    }

    /// <summary>
    /// Reach a queue node.
    /// </summary>
    /// <param name="nodeId">The id of the node.</param>
    /// <param name="agvCorrelationId">The agv that reaches the node.</param>
    public void ReachNode(string nodeId, Guid agvCorrelationId)
    {
        var node = Nodes.SingleOrDefault(n => n.NodeId == nodeId);
        if (node == null)
        {
            return;
        }
        node.Occupy(agvCorrelationId);
    }

    /// <summary>
    /// Remove an agv from the queue without updating other agvs positions.
    /// </summary>
    public void RemoveAgv(Guid agvCorrelationId)
    {
        var node = Nodes.SingleOrDefault(n => n.AgvIsAssigned(agvCorrelationId));
        if (node != null) // Agv already enqueued.
        {
            node.Free(agvCorrelationId);
        }
        
        AgvFillQuantity -= 1;
        AgvFillQuantity = Math.Max(AgvFillQuantity, 0);
    }
}
