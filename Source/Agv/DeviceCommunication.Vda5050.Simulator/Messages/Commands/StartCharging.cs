using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;

/// <summary>
/// Provides a command to start charging.
/// </summary>
/// <param name="CorrelationId">the correlation if the agv.</param>
public record StartCharging(Guid CorrelationId) : CorrelatedBy<Guid>;