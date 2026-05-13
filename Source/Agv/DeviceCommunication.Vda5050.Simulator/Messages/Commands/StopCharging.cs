using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;

/// <summary>
/// Provides a command to stop charging.
/// </summary>
/// <param name="CorrelationId">the correlation if the agv.</param>
public record StopCharging(Guid CorrelationId) : CorrelatedBy<Guid>;