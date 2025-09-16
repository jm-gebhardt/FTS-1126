using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Node;

/// <summary>
/// Node State
/// </summary>
public enum NodeLockState
{
    /// <summary>Node is Free to use</summary>
    Free,

    /// <summary>Node is Reserved for an agv</summary>
    Reserved,

    /// <summary>Node is occupied by an agv</summary>
    Occupied
}

/// <summary>
/// Lock state of a node
/// </summary>
[PublicAPI]
public record NodeLock
{
    /// <summary>
    /// The id of the node.
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// The node.
    /// </summary>
    public GraphNode Node { get; set; }

    /// <summary>
    /// The state the node is in.
    /// </summary>
    public NodeLockState NodeState { get; set; }

    /// <summary>
    /// True, if the node is locked.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// The agv assigned to the node.
    /// </summary>
    public Guid? AgvCorrelationId { get; set; }

    /// <summary>
    /// The time the node lock was last modified.
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// EF
    /// </summary>
    public NodeLock()
    {
        Node = null!;
    }

    /// <inheritdoc cref="NodeLock"/>>
    public NodeLock(GraphNode node, NodeLockState nodeState, bool isLocked, Guid? agvCorrelationId, DateTime modifiedDate)
    {
        Node = node;
        NodeId = node.Id;
        NodeState = nodeState;
        IsLocked = isLocked;
        AgvCorrelationId = agvCorrelationId;
        ModifiedDate = modifiedDate;
    }

    /// <summary>
    /// True, if the node is occupied by the given agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <returns></returns>
    public bool IsOccupiedByAgv(Guid agvCorrelationId)
    {
        return AgvCorrelationId == agvCorrelationId && NodeState == NodeLockState.Occupied;
    }

    /// <summary>
    /// True, if the node is reserved for the given agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <returns></returns>
    public bool IsReservedForAgv(Guid agvCorrelationId)
    {
        return AgvCorrelationId == agvCorrelationId && NodeState == NodeLockState.Reserved;
    }
}