using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;

/// <summary>
/// Represents a command to merge a graph segment into the stored graph.
/// </summary>
/// <param name="CorrelationId">The identifier of this message.</param>
/// <param name="GraphSegment">The graph segment to save.</param>
public record AddNewGraphSegment(Guid CorrelationId, GraphSegment GraphSegment) : CorrelatedBy<Guid>;