using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.MessageContent;

/// <summary>
/// Message content for a node in the graph.
/// </summary>
[PublicAPI]
public record GraphNode
{
    /// <summary>
    /// The unique Id of the node.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The Name of the node.
    /// </summary>
    public string Name
    {
        get;
        [UsedImplicitly]
        init;
    } = null!;

    /// <summary>
    /// The type of the node
    /// </summary>
    public AgvGraph.NodeType Type
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// The X coordinate of the node
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// The Y coordinate of the node
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// The Z coordinate of the node
    /// </summary>
    public float Z { get; init; }

    /// <summary>
    /// The Theta coordinate of the node
    /// </summary>
    public float Theta { get; init; }

    /// <summary>
    /// The drop lift height, if set.
    /// </summary>
    public double? DropLiftHeight { get; init; }

    /// <summary>
    /// The pick lift height, if set.
    /// </summary>
    public double? PickLiftHeight { get; init; }

    /// <summary>
    /// The docking direction of the node, if set.
    /// </summary>
    public DockingDirection? DockingDirection { get; init; }
}