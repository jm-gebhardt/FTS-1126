using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// AcceptAgv was successful.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
public record AcceptAgvSucceeded(Guid CorrelationId) : CorrelatedBy<Guid>;
