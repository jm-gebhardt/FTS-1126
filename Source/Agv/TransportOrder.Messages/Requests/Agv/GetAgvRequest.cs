using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Requests the Agv with the given CorrelationId from the system. The Agv is high quality, meaning it comes directly from the state machine and not from the database, so it is most recent.
/// </summary>
/// <param name="CorrelationId"></param>
public record GetAgvRequest(Guid CorrelationId) : CorrelatedBy<Guid>;