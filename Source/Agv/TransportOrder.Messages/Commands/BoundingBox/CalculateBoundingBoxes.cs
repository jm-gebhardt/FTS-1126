using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.BoundingBox;

/// <summary>
/// Represents a command to recalculate bounding boxes for all known AGV types after a graph change.
/// </summary>
[PublicAPI]
public record CalculateBoundingBoxes(Guid CorrelationId, Graph Graph) : CorrelatedBy<Guid>;