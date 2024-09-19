using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Provides a command to trigger the mode change.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record SwitchMode(Guid CorrelationId): CorrelatedBy<Guid>;