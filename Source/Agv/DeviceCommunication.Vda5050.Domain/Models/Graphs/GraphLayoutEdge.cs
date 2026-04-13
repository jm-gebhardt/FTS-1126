using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;

/// <summary>
/// An edge within a <see cref="GraphLayout"/>.
/// </summary>
[PublicAPI]
public record GraphLayoutEdge
{
    /// <summary>
    /// Unique identifier for this edge.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Fleet-level edge identifier.
    /// </summary>
    public string EdgeId { get; init; } = string.Empty;

    /// <summary>
    /// The node id of the start node.
    /// </summary>
    public string StartNodeId { get; init; } = string.Empty;

    /// <summary>
    /// The node id of the end node.
    /// </summary>
    public string EndNodeId { get; init; } = string.Empty;

    /// <summary>
    /// The time the node was created.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Vehicle-type-specific properties for this edge.
    /// </summary>
    public ICollection<EdgeVehicleTypeProperty> VehicleTypeProperties { get; init; } = [];
}