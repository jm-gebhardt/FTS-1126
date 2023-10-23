using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to set the agv in charging state.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record StartCharging(Guid CorrelationId) : CorrelatedBy<Guid>;