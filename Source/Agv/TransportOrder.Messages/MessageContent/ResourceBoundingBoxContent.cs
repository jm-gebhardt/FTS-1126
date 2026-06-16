using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.MessageContent;

/// <summary>
/// Message content carrying the bounding box polygon for one graph resource and AGV type.
/// </summary>
[PublicAPI]
public record ResourceBoundingBoxContent
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
    public IReadOnlyList<PolygonPoint> Polygon { get; init; } = [];
}