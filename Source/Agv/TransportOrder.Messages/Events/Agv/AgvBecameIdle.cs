using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Agv became idle.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
public record AgvBecameIdle(Guid CorrelationId) : CorrelatedBy<Guid>;