using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents an event raised when an agv has charged to the minimum charge threshold.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
public record AgvBatteryChargedToMinimum(Guid CorrelationId): CorrelatedBy<Guid>;