using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to stop the agv charging.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record StopCharging(Guid CorrelationId): CorrelatedBy<Guid>;