using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;

/// <summary>
/// Response carrying the requested bounding box polygons.
/// </summary>
[PublicAPI]
public record GotBoundingBoxes(IReadOnlyList<ResourceBoundingBoxContent> BoundingBoxes);