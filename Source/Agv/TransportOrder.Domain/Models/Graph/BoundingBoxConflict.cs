using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Graph;

/// <summary>
/// A pair of graph resources whose bounding boxes geometrically intersect.
/// </summary>
[PublicAPI]
public record BoundingBoxConflict(ResourceBoundingBox ResourceA, ResourceBoundingBox ResourceB);