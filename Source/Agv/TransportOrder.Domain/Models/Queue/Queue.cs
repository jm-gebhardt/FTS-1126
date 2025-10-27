using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Queue;

/// <summary>
/// Represents a queue
/// </summary>
public class Queue
{
    /// <summary>
    /// The order id use for evacuation-
    /// </summary>
    public static string EvacuationOrderId = "Queue Evacuation Move";

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

    private QueueNode? QueueHead => Nodes.MinBy(n => n.Position);

    /// <summary>
    ///  The node at the position from which to dequeue if the queue isn't empty.
    /// </summary>
    public QueueNode? Head
    {
        get
        {
            bool isOccupied = QueueHead?.OccupiedAgv != null;
            if (isOccupied)
            {
                return QueueHead;
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
    public Guid? EntryNode => Nodes.OrderBy(n => n.Position).LastOrDefault()?.NodeId;

    /// <summary>
    /// All the nodes of the queue that are not the entry node.
    /// </summary>
    public ICollection<Guid> NonEntryNodes => Nodes.Select(q => q.NodeId).Where(n => n != EntryNode).ToList();

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

    /// <summary>
    /// The agv ids that need to be considered for ordering in this queue. Includes non-queueing agvs.
    /// The higher the index the farther through the queue the agv is.
    /// </summary>
    public RandomAccessStack<Guid> ConsideredAgvs = new();

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

    private void IncrementAgvFillAmount()
    {
        AgvFillQuantity = Math.Min(AgvFillQuantity + 1, Nodes.Count);
    }

    private void DecreaseAgvFillQuantity()
    {
        AgvFillQuantity = Math.Max(0, AgvFillQuantity - 1);
    }

    /// <summary>
    /// Try to enter a queue.
    /// </summary>
    /// <returns>Id of entry node.</returns>
    public Guid? TryEnterQueue()
    {
        if (IsFull)
        {
            return null;
        }
        IncrementAgvFillAmount();
        return EntryNode;
    }

    /// <summary>
    /// Add a node to a queue.
    /// </summary>
    /// <param name="nodeId">The node id.</param>
    /// <param name="position">The position of the node in the queue.</param>
    public void AddNode(Guid nodeId, int position)
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
    public void RemoveNode(Guid nodeId)
    {
        var node = Nodes.SingleOrDefault(n => n.NodeId == nodeId);
        if (node == null)
        {
            throw new NodeNotFoundException(nodeId);
        }
        Nodes.Remove(node);
        if (node.OccupiedAgv != null)
        {
            DecreaseAgvFillQuantity();
        }
    }

    /// <summary>
    /// Enqueue an agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv to enqueue.</param>
    /// <param name="targetNode">The id of the target node to enqueue to.</param>
    public bool TryEnqueue(Guid agvCorrelationId, [NotNullWhen(true)] out Guid? targetNode)
    {
        if (AgvFillQuantity == 0)
        {
            targetNode = null;
            return false;
        }

        ConsideredAgvs.Push(agvCorrelationId);

        if (AllAgvsEntered && !IsEnqueued(agvCorrelationId))
        {
            targetNode = EntryNode!.Value;
            return true;
        }
        int tail = Tail!.Position;
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
    public Guid? TryDequeue(out Dictionary<Guid, Guid> agvsToMove)
    {
        agvsToMove = new Dictionary<Guid, Guid>();
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
        AgvFillQuantity = Math.Max(0, AgvFillQuantity - 1);
        return agv;
    }

    /// <summary>
    /// Shift agvs down in the queue.
    /// </summary>
    /// <param name="agvsToMove">Output: agvs to move one position down.</param>
    /// <param name="startPosition">The position from which to start moving down.</param>
    private void ShiftDown(ref Dictionary<Guid, Guid> agvsToMove, int startPosition)
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
    /// True, if the head node is occupied by the given agv.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    public bool IsAgvAtQueueHead(Guid agvCorrelationId)
    {
        return QueueHead != null && QueueHead.OccupiedAgv == agvCorrelationId;
    }

    /// <summary>
    /// Escape the queue.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv that escapes.</param>
    /// <param name="agvsToMove">The Output: agvs to move one position down</param>
    /// <returns>Node at which the queue was escaped.</returns>
    public Guid? TryEscapeQueue(Guid agvCorrelationId, out Dictionary<Guid, Guid> agvsToMove)
    {
        agvsToMove = new Dictionary<Guid, Guid>();
        var node = Nodes.SingleOrDefault(n => n.OccupiedAgv == agvCorrelationId);
        if (node == null)
        {
            node = Nodes.OrderBy(n => n.Position).FirstOrDefault(n => n.ReservedAgv == agvCorrelationId);
            if (node == null)
            {
                return null;
            }
        }
        ShiftDown(ref agvsToMove, node.Position);
        node.RemoveAgv(agvCorrelationId);
        //Remove agv from all queue nodes
        foreach (var reservedNode in Nodes.Where(n => n.GetCurrentAgv() == agvCorrelationId))
        {
            reservedNode.RemoveAgv(agvCorrelationId);
        }
        AgvFillQuantity = Math.Max(0, AgvFillQuantity - 1);
        return node.NodeId;
    }

    /// <summary>
    /// Try swap one agv for another.
    /// </summary>
    /// <param name="queueingAgv">The correlation id of the agv that already queues.</param>
    /// <param name="agvsToMove">The Output: agvs to move one position down</param>
    public void TrySwapAgv(Guid queueingAgv, out Dictionary<Guid, Guid> agvsToMove)
    {
        agvsToMove = new Dictionary<Guid, Guid>();
        var node = Nodes.SingleOrDefault(n => n.OccupiedAgv == queueingAgv);
        if (node == null)
        {
            return;
        }
        ShiftDown(ref agvsToMove, node.Position);
        node.RemoveAgv(queueingAgv);
        //Remove agv from all queue nodes
        foreach (var reservedNode in Nodes.Where(n => n.GetCurrentAgv() == queueingAgv))
        {
            reservedNode.RemoveAgv(queueingAgv);
        }
        // Do not update fill size, as another agv is going to replace it
    }

    /// <summary>
    /// Reach a queue node.
    /// </summary>
    /// <param name="nodeId">The id of the node.</param>
    /// <param name="agvCorrelationId">The agv that reaches the node.</param>
    public void ReachNode(Guid nodeId, Guid agvCorrelationId)
    {
        var node = Nodes.SingleOrDefault(n => n.NodeId == nodeId);
        if (node == null)
        {
            return;
        }
        if (Nodes.Any(n => n.NodeId != node.NodeId && n.GetCurrentAgv() == agvCorrelationId && n.Position < node.Position))
        {
            // do not occupy if another downstream node has already been reserved for this agv
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
        node?.Free(agvCorrelationId); // Agv might already be dequeued.

        AgvFillQuantity = Math.Max(0, AgvFillQuantity - 1);
    }
}