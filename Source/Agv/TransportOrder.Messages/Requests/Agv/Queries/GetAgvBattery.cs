using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// 
/// </summary>
/// <param name="CorrelationId"></param>
public record GetAgvBattery(Guid CorrelationId) : CorrelatedBy<Guid>;