using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Agv became idle.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
public record AgvBecameIdle(Guid CorrelationId) : CorrelatedBy<Guid>;