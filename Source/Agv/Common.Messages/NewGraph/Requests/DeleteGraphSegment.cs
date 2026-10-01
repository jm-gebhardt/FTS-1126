using MassTransit;

namespace Gebhardt.Acc.Common.Messages.NewGraph.Requests;

/// <summary>
/// Represents a request to delete a GraphSegment
/// </summary>
/// <param name="CorrelationId">The identifier of this message.</param>
/// <param name="GraphSegmentId">The graph segment id to be deleted.</param>
public record DeleteGraphSegment(Guid CorrelationId, Guid GraphSegmentId) : CorrelatedBy<Guid>;