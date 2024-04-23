using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Request to get the AgvRoute for the specified AgvCorrelationId
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv</param>
public record GetCurrentAgvRoute(Guid CorrelationId) : CorrelatedBy<Guid>;