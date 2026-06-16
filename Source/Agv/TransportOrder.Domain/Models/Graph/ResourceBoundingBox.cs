using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Graph;

/// <summary>
/// The oriented bounding box polygon for one graph resource used by a specific AGV type.
/// </summary>
[PublicAPI]
public record ResourceBoundingBox
{
    /// <summary>
    /// The unique identifier of this bounding box record.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The ID of the <see cref="GraphNode"/> or <see cref="GraphEdge"/> this box belongs to.
    /// </summary>
    public Guid ResourceId { get; init; }

    /// <summary>
    /// The type of the resource this box belongs to.
    /// </summary>
    public ResourceType ResourceType { get; init; }

    /// <summary>
    /// The AGV type for which this bounding box was computed.
    /// </summary>
    public string AgvType { get; init; } = string.Empty;

    /// <summary>
    /// The polygon, including all configured margins.
    /// </summary>
    public Polygon Polygon { get; init; } = new([]);

    /// <summary>
    /// The UTC time at which this bounding box was last calculated.
    /// </summary>
    public DateTime CalculatedAt { get; init; }
}