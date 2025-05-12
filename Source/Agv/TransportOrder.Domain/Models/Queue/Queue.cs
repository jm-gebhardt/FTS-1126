using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;

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
            bool isOccupied = node?.State?.NodeState == NodeLockState.Occupied;
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
    public QueueNode? Tail => Nodes.OrderBy(n => n.Position).FirstOrDefault(n => n.State is { NodeState: NodeLockState.Free, IsLocked: false });

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
    public bool AllAgvsEntered => Nodes.Count(n => n.State?.AgvCorrelationId != null) == Nodes.Count;

    /// <summary>
    /// The agvs in the queue, ordered from entry node to head.
    /// </summary>
    public List<Guid> Agvs => Nodes.OrderByDescending(n => n.Position).Where(n => n.State is { AgvCorrelationId: not null }).Select(n => n.State!.AgvCorrelationId!.Value).ToList();
    
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
        ResolveDuplications();
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
        node.Reserve(agvCorrelationId);
        targetNode = node.NodeId;
        return true;
    }

    /// <summary>
    /// If there are multiple not free nodes with the same agv,
    /// set state of node with higher position to "free".
    /// This can happen, when enqueuing is invoked shortly after
    /// escaping/dequeuing if the agv did not move yet, but was already
    /// assigned to move in the queue. In that case for the agv there could
    /// be an occupied and a reserved node in the queue.
    /// </summary>
    private void ResolveDuplications()
    {
        IEnumerable<IGrouping<Guid?, QueueNode>> nodeGroup = Nodes
            .Where(n => n.State?.AgvCorrelationId != null)
            .GroupBy(n => n.State?.AgvCorrelationId)
            .Where(g => g.Count() > 1);

        foreach (IGrouping<Guid?, QueueNode> group in nodeGroup)
        {
            List<QueueNode> orderedNodes = group.OrderByDescending(n => n.Position).ToList();
            for (int i = 0; i < orderedNodes.Count - 1; i++)
            {
                var node = orderedNodes[i];
                if (node.State != null)
                {
                    node.State = node.State with
                    {
                        AgvCorrelationId = null,
                        NodeState = NodeLockState.Free,
                        IsLocked = false
                    };
                }
            }
        }
    }

    /// <summary>
    /// Dequeue an agv.
    /// </summary>
    /// <param name="agvsToMove">Output: Agvs that are to move down the queue and their target node ids.</param>
    public Guid? TryDequeue(out Dictionary<Guid, string> agvsToMove)
    {
        ResolveDuplications();
        agvsToMove = new Dictionary<Guid, string>();
        if (AgvFillQuantity == 0 || Head == null)
        {
            return null;
        }
        int head = Head!.Position;
        var node = Nodes.Single(n => n.Position == head);
        if (node.State?.AgvCorrelationId == null)
        {
            return null;
        }
        var agv = node.State!.AgvCorrelationId!.Value;
        if (!TryShiftDown(ref agvsToMove, head))
        {
            return null;
        }
        AgvFillQuantity -= 1;
        AgvFillQuantity = Math.Max(AgvFillQuantity, 0);
        return agv;
    }

    /// <summary>
    ///  Try to shift agvs down in the queue.
    /// </summary>
    /// <param name="agvsToMove">Output: agvs to move one position down.</param>
    /// <param name="startPosition">The position from which to start moving down.</param>
    /// <returns>True, if shifting was successful</returns>
    private bool TryShiftDown(ref Dictionary<Guid, string> agvsToMove, int startPosition)
    {
        foreach (var node in Nodes.Where(n => n.Position > startPosition && n.State?.NodeState != NodeLockState.Free).OrderBy(n => n.Position))
        {
            if (node.State?.AgvCorrelationId == null)
            {
                agvsToMove =  new Dictionary<Guid, string>();
                return false;
            }
            var agv = node.State!.AgvCorrelationId!.Value;
            var target = Nodes.Single(n => n.Position == node.Position - 1);
            target.Reserve(agv);
            agvsToMove.Add(agv, target.NodeId);
        }
        return true;
    }

    /// <summary>
    /// True, if an agv has already been enqueued.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    public bool IsEnqueued(Guid agvCorrelationId)
    {
        return Nodes.Any(n => n.State?.AgvCorrelationId == agvCorrelationId && n.State?.NodeState != NodeLockState.Free);
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
        return lastNode?.State?.AgvCorrelationId == agvCorrelationId && lastNode.NodeId == nodeId;
    }

    /// <summary>
    /// Escape the queue.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv that escapes.</param>
    /// <param name="agvsToMove">The Output: agvs to move one position down</param>
    /// <returns>Node at which the queue was escaped.</returns>
    public string? TryEscapeQueue(Guid agvCorrelationId, out Dictionary<Guid, string> agvsToMove)
    {
        ResolveDuplications();
        agvsToMove = new Dictionary<Guid, string>();
        var node = Nodes.SingleOrDefault(n => n.State?.AgvCorrelationId == agvCorrelationId && n.State?.NodeState != NodeLockState.Free);
        if (node == null)
        {
            return null;
        }
        if (!TryShiftDown(ref agvsToMove, node.Position))
        {
            return null;
        }
        AgvFillQuantity -= 1;
        AgvFillQuantity = Math.Max(AgvFillQuantity, 0);
        return node.NodeId;
    }

    /// <summary>
    /// Remove an agv from the queue without updating other agvs positions.
    /// </summary>
    public void RemoveAgv()
    {
        AgvFillQuantity -= 1;
        AgvFillQuantity = Math.Max(AgvFillQuantity, 0);
    }
}
