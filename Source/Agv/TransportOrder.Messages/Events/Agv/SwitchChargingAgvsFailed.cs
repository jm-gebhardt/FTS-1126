using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised when switching an agv not currently charging with one that is failed.
/// </summary>
/// <param name="CorrelationId">The agv not currently charging.</param>
/// <param name="ChargingAgv">The correlation id of the  charging agv that should stop charging.</param>
/// <param name="RetryDelaySec">The timespan in seconds waiting before retrying the switch of agvs.</param>
public record SwitchChargingAgvsFailed(Guid CorrelationId, Guid ChargingAgv, double RetryDelaySec=60): CorrelatedBy<Guid>;