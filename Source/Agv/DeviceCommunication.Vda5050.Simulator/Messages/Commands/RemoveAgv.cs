using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;

/// <summary>
/// Provides a command to remove a simulated agv.
/// </summary>
/// <param name="CorrelationId">the correlation of the agv.</param>
public record RemoveAgv(Guid CorrelationId) : CorrelatedBy<Guid>;