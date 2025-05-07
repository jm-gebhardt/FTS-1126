using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Common.Messages.ActionGroups;

/// <summary>
/// The target of the action.
/// </summary>
public record ActionTarget
{
    /// <summary>
    /// Target node id of the action
    /// </summary>
    public string? NodeId { get; set; }

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
    public ActionTarget(string targetNodeId)
    {
        NodeId = targetNodeId;
        Position = null;
    }

    /// <summary>
    /// The target position of the agv.
    /// </summary>
    public ActionTarget(float x, float y, float theta)
    {
        Position = new AgvPosition(true, x, y, 0, theta);
        NodeId = null;
    }

    /// <summary>
    /// The target position of the agv.
    /// </summary>
    [JsonConstructor]
    public ActionTarget(string? nodeId, AgvPosition? position)
    {
        NodeId = nodeId;
        Position = position;
    }
}
