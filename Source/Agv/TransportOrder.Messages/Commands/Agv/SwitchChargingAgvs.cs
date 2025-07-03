using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to switch an agv not currently charging with one that is.
/// </summary>
/// <param name="NonChargingAgv">The agv not currently charging.</param>
/// <param name="CorrelationId">The correlation id of the  charging agv that should stop charging.</param>
public record SwitchChargingAgvs(Guid NonChargingAgv, Guid CorrelationId): CorrelatedBy<Guid>;