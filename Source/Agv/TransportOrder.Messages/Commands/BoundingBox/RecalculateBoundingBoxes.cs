using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.BoundingBox;

/// <summary>
/// Represents a command to recalculate the bounding boxes of every persisted graph for all known AGV types.
/// </summary>
[PublicAPI]
public record RecalculateBoundingBoxes(Guid CorrelationId) : CorrelatedBy<Guid>;