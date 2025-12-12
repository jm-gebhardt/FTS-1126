using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// The target of an action.
/// </summary>
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
    [JsonConstructor]
    public ActionTarget(GraphNode? node, AgvPosition? position)
    {
        Node = node;
        Position = position;
    }
}