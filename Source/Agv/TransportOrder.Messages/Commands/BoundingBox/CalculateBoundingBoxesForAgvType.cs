using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.BoundingBox;

/// <summary>
/// Represents a command to calculate bounding boxes for a newly registered AGV type.
/// </summary>
[PublicAPI]
public record CalculateBoundingBoxesForAgvType(Guid CorrelationId, AgvData AgvData) : CorrelatedBy<Guid>;