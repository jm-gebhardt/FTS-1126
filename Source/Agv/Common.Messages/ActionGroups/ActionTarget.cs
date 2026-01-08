using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.ActionGroups;

/// <summary>
/// The target of the action.
/// </summary>
[PublicAPI]
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

    /// <summary>
    /// Optional target stations, if the target is not decided.
    /// </summary>
    public ICollection<string> StationOptions { get; set; } = [];

    /// <summary>
    /// The Doppelspiel mode of the target.
    /// </summary>
    public DoppelspielMode DoppelspielMode { get; set; } = DoppelspielMode.None;

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
        StationOptions = [];
        DoppelspielMode = DoppelspielMode.None;
        Node = null;
    }

    /// <summary>
    /// The target position of the agv.
    /// </summary>
    [JsonConstructor]
    public ActionTarget(GraphNode? node, AgvPosition? position, ICollection<string>? stationOptions = null, DoppelspielMode doppelspielMode = DoppelspielMode.None)
    {
        Node = node;
        Position = position;
        StationOptions = stationOptions ?? [];
        DoppelspielMode = doppelspielMode;
    }
}