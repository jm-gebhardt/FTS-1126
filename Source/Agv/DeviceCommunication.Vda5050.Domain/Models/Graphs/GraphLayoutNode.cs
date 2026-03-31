using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;

/// <summary>
/// A node within a <see cref="GraphLayout"/>.
/// </summary>
[PublicAPI]
public record GraphLayoutNode
{
    /// <summary>
    /// Unique identifier for this node.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Fleet-level node identifier.
    /// </summary>
    public string NodeId { get; init; } = string.Empty;

    /// <summary>
    /// Optional human-readable name.
    /// </summary>
    public string? NodeName { get; init; }

    /// <summary>
    /// The map this node belongs to. Falls back to the layout's LayoutId when not set.
    /// </summary>
    public string? MapId { get; init; }

    /// <summary>
    /// X position on the layout in reference to the global origin.
    /// </summary>
    public double PositionX { get; init; }

    /// <summary>
    /// Y position on the layout in reference to the global origin.
    /// </summary>
    public double PositionY { get; init; }

    /// <summary>
    /// The Theta coordinate of the node.
    /// </summary>
    public float Theta { get; init; }

    /// <summary>
    /// Vehicle-type-specific properties for this node.
    /// </summary>
    public ICollection<NodeVehicleTypeProperty> VehicleTypeProperties { get; init; } = [];
}