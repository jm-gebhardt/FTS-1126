using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.GraphAnalysis;

/// <summary>
/// Carries the bounding box polygon for one graph resource and AGV type.
/// </summary>
[PublicAPI]
public record ResourceBoundingBoxDto
{
    /// <summary>
    /// The ID of the graph node or edge this box belongs to.
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
    /// The polygon vertices in CCW order, including all configured margins.
    /// </summary>
    public IReadOnlyList<PolygonPointDto> Polygon { get; init; } = [];
}