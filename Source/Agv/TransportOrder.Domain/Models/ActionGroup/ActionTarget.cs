using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// The target of an action.
/// </summary>
[PublicAPI]
public record ActionTarget
{
    /// <summary>
    /// The NodeId of the target node.
    /// </summary>
    public Guid? NodeId { get; set; }

    /// <summary>
    /// Target node of the action.
    /// </summary>
    public GraphNode? Node { get; set; }

    /// <summary>
    /// The target position of the agv (in case the Node is not set).
    /// </summary>
    public AgvPosition? Position { get; }

    /// <summary>
    /// Optional target stations, if the target is not decided.
    /// </summary>
    public ICollection<string> StationOptions { get; set;  } = [];

    /// <summary>
    /// The Doppelspiel mode of the target.
    /// </summary>
    public DoppelspielMode DoppelspielMode { get; set; } = DoppelspielMode.None;

    /// <inheritdoc cref="ActionTarget"/>>
    public ActionTarget()
    {
    }

    /// <inheritdoc cref="ActionTarget"/>>
    public ActionTarget(GraphNode? targetNode)
    {
        Node = targetNode;
        NodeId = targetNode?.Id;
        Position = null;
    }

    /// <inheritdoc cref="ActionTarget"/>>
    public ActionTarget(float x, float y, float theta)
    {
        Position = new AgvPosition(true, x, y, 0, theta);
        Node = null;
    }

    /// <inheritdoc cref="ActionTarget"/>>
    public ActionTarget(GraphNode? node, AgvPosition? position)
    {
        Node = node;
        NodeId = node?.Id;
        Position = position;
    }

    /// <inheritdoc cref="ActionTarget"/>>
    [JsonConstructor]
    public ActionTarget(GraphNode? node, AgvPosition? position, ICollection<string>? stationOptions = null, DoppelspielMode doppelspielMode = DoppelspielMode.None)
    {
        Node = node;
        NodeId = node?.Id;
        Position = position;
        StationOptions = stationOptions ?? [];
        DoppelspielMode = doppelspielMode;
    }

    /// <summary>
    /// Determines equality based on the target location (NodeId/Position), not metadata like StationOptions or DoppelspielMode.
    /// </summary>
    public virtual bool Equals(ActionTarget? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (NodeId.HasValue && other.NodeId.HasValue)
        {
            return NodeId.Value == other.NodeId.Value;
        }

        if (Position != null && other.Position != null)
        {
            return Position.Equals(other.Position);
        }

        if (!NodeId.HasValue && Position == null && !other.NodeId.HasValue && other.Position == null)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets hash code based on the target location (NodeId/Position).
    /// </summary>
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        if (NodeId.HasValue)
        {
            return NodeId.Value.GetHashCode();
        }

        if (Position != null)
        {
            return Position.GetHashCode();
        }

        return 0;
    }
}