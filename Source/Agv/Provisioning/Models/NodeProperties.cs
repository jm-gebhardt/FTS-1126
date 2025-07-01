using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Provisioning.Models;

/// <summary>
/// The node properties.
/// </summary>
[PublicAPI]
public class NodeProperties
{
    /// <inheritdoc cref="NodeProperties"/>>
    public NodeProperties(string nodeId, double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection)
    {
        NodeId = nodeId;
        PickLiftHeight = pickLiftHeight;
        DropLiftHeight = dropLiftHeight;
        DockingDirection = dockingDirection;
    }

    /// <summary>
    /// The node id.
    /// </summary>
    public string NodeId { get; init; }

    /// <summary>
    /// The pick lift height, if set.
    /// </summary>
    public double? PickLiftHeight { get; init; }

    /// <summary>
    /// The drop lift height, if set.
    /// </summary>
    public double? DropLiftHeight { get; init; }

    /// <summary>
    /// The docking direction of the node, if set.
    /// </summary>
    public DockingDirection? DockingDirection { get; init; }
}