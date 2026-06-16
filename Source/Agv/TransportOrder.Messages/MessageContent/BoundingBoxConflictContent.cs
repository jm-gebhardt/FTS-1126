using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.MessageContent;

/// <summary>
/// Message content with a pair of graph resources whose bounding boxes intersect.
/// </summary>
[PublicAPI]
public record BoundingBoxConflictContent(Guid ResourceAId, Guid ResourceBId);