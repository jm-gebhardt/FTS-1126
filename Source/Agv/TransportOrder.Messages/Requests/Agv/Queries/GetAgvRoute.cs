using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Request to get the AgvRoute for the specified AgvCorrelationId
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv</param>
public record GetAgvRoute(Guid CorrelationId) : CorrelatedBy<Guid>;