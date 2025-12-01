using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// The target of the action.
/// </summary>
public record ActionTarget
{
    /// <summary>
    /// Target node id of the action
    /// </summary>
    public GraphNode? Node { get; set; }

    /// <summary>
    /// The target position of the agv.
    /// </summary>
    public AgvPosition? Position { get; }

    /// <inheritdoc cref="ActionTarget"/>>
    public ActionTarget()
    {
    }

    /// <summary>
    /// The target position of the agv.
    /// </summary>
    public ActionTarget(GraphNode? targetNode)
    {
        Node = targetNode;
        Position = null;
    }

    /// <summary>
    /// The target position of the agv.
    /// </summary>
    public ActionTarget(float x, float y, float theta)
    {
        Position = new AgvPosition(true, x, y, 0, theta);
        Node = null;
    }

    /// <summary>
    /// The target position of the agv.
    /// </summary>
    [JsonConstructor]
    public ActionTarget(GraphNode? node, AgvPosition? position)
    {
        Node = node;
        Position = position;
    }
}