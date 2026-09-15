using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.BoundingBoxes;

/// <summary>
/// Published after the bounding boxes of all known AGV types have been recalculated.
/// </summary>
[PublicAPI]
public record BoundingBoxesRecalculated(Guid CorrelationId) : CorrelatedBy<Guid>;