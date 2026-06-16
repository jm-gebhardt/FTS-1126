using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.GraphAnalysis;

/// <summary>
/// A pair of graph resources whose bounding boxes intersect.
/// </summary>
[PublicAPI]
public record BoundingBoxConflictDto(Guid ResourceAId, Guid ResourceBId);