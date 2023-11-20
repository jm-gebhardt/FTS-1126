using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Request to get the IsLoaded state for the specified AgvCorrelationIds
/// </summary>
/// <param name="CorrelationId"></param>
public record IsAgvLoadedRequest(Guid CorrelationId) : CorrelatedBy<Guid>;