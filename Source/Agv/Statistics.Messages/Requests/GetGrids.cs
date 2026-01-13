using MassTransit;

namespace Gebhardt.Acc.Statistics.Messages.Requests;

/// <summary>
/// Provides a request to get all grids.
/// </summary>
/// <param name="CorrelationId">The correlation id of the request.</param>
public record GetGrids(Guid CorrelationId) : CorrelatedBy<Guid>;