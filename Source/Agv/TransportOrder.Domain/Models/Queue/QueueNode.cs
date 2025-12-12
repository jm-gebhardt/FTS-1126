using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Queue;

/// <summary>
/// A node in a queue
/// </summary>
public class QueueNode
{
    /// <summary>
    /// The id of the node.
    /// </summary>
    public Guid NodeId
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// The position of the node in the queue.
    /// </summary>
    public int Position
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// The corresponding <see cref="GraphNode"/>.
    /// </summary>
    public GraphNode? Node { get; set; }

    /// <summary>
    /// The agv the node is reserved for.
    /// </summary>
    public Guid? ReservedAgv { get; private set; }

    /// <summary>Indicates whether the reserved AGV has changed.</summary>
    public bool ReservedAgvChanged { get; private set; }

    /// <summary>
    /// The time the node was last reserved.
    /// </summary>
    public DateTime? LastReservedDate { get; private set; }

    /// <summary>
    /// The agv that occupies the queue node.
    /// </summary>
    public Guid? OccupiedAgv { get; private set; }

    /// <summary>Indicates if the occupancy state of the AGV has changed.</summary>
    public bool OccupiedAgvChanged { get; private set; }

    /// <summary>
    /// The time the node was last occupied.
    /// </summary>
    public DateTime? LastOccupiedDate { get; private set; }

    /// <inheritdoc cref="QueueNode"/>>
    [JsonConstructor]
    public QueueNode()
    {
    }

    /// <inheritdoc cref="QueueNode"/>>
    public QueueNode(Guid nodeId, int position)
    {
        NodeId = nodeId;
        Position = position;
    }

    /// <summary>
    /// Reserve a queue node for an agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv to reserve the queue node for.</param>
    public void Reserve(Guid agvCorrelationId)
    {
        if (ReservedAgv == agvCorrelationId)
        {
            return;
        }
        ReservedAgv = agvCorrelationId;
        ReservedAgvChanged = true;
        LastReservedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Occupy the queue node.
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    public void Occupy(Guid agvCorrelationId)
    {
        if (OccupiedAgv != agvCorrelationId)
        {
            OccupiedAgv = agvCorrelationId;
            OccupiedAgvChanged = true;
            LastOccupiedDate = DateTime.UtcNow;
        }
        if (ReservedAgv == OccupiedAgv)
        {
            ReservedAgv = null;
            ReservedAgvChanged = true;
            LastReservedDate = null;
        }
    }

    /// <summary>
    /// Free the queue node.
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    public void Free(Guid agvCorrelationId)
    {
        if (OccupiedAgv != agvCorrelationId)
        {
            return;
        }
        OccupiedAgv = null;
        OccupiedAgvChanged = true;
        LastOccupiedDate = null;
    }

    /// <summary>
    /// Remove an agv from a node.
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    public void RemoveAgv(Guid agvCorrelationId)
    {
        if (ReservedAgv == agvCorrelationId)
        {
            ReservedAgv = null;
            ReservedAgvChanged = true;
            LastReservedDate = null;
        }
        if (OccupiedAgv == agvCorrelationId)
        {
            OccupiedAgv = null;
            OccupiedAgvChanged = true;
            LastOccupiedDate = null;
        }
    }

    /// <summary>
    /// True, if the agv is reserved for or already arrived at a node.
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <returns>True, if the agv has a reserved or occupied node in the queue.</returns>
    public bool AgvIsAssigned(Guid agvCorrelationId)
    {
        return OccupiedAgv == agvCorrelationId || ReservedAgv == agvCorrelationId;
    }

    /// <summary>
    /// The agv that either occupied or the reserved the node last.
    /// </summary>
    /// <returns>The correlation id of the agv.</returns>
    public Guid? GetCurrentAgv()
    {
        if (LastOccupiedDate == null && LastReservedDate == null)
        {
            return null;
        }
        if (LastOccupiedDate > LastReservedDate || LastReservedDate == null)
        {
            return OccupiedAgv;
        }
        return ReservedAgv;
    }

    /// <summary>Resets the changed state flags for the node.</summary>
    public void ClearChangedFlags()
    {
        ReservedAgvChanged = false;
        OccupiedAgvChanged = false;
    }
}