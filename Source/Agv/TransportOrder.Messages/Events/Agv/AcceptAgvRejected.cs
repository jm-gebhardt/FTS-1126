using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// AcceptAgv was rejected.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
public record AcceptAgvRejected(Guid CorrelationId) : CorrelatedBy<Guid>;