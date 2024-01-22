using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Request to get the HasCargo state for the specified AgvCorrelationId.
/// </summary>
/// <param name="CorrelationId"></param>
public record GetAgvHasCargo(Guid CorrelationId) : CorrelatedBy<Guid>;