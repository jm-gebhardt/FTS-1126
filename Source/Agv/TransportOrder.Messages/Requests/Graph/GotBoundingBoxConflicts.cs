using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;

/// <summary>
/// Response carrying the detected bounding box conflicts.
/// </summary>
[PublicAPI]
public record GotBoundingBoxConflicts(IReadOnlyList<BoundingBoxConflictContent> Conflicts);