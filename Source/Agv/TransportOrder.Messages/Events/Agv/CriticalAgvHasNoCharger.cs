using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents an event raised when a critical agv has no charger available.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
public record CriticalAgvHasNoCharger(Guid CorrelationId): CorrelatedBy<Guid>;