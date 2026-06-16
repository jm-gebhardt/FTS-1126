using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;

/// <summary>
/// Represents a command to retrieve bounding boxes for a given AGV type, optionally filtered to a single resource or resource type.
/// </summary>
[PublicAPI]
public record GetBoundingBoxes(Guid CorrelationId, string AgvType, Guid? ResourceId) : CorrelatedBy<Guid>;