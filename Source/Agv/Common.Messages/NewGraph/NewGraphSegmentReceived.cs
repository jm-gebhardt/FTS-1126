using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.NewGraph;

/// <summary>
/// Represents an event raised when a new graph segment has been received.
/// </summary>
/// <param name="CorrelationId">The identifier of this message.</param>
/// <param name="GraphSegment">The graph segment that was received.</param>
public record NewGraphSegmentReceived(Guid CorrelationId, GraphSegment GraphSegment) : CorrelatedBy<Guid>;