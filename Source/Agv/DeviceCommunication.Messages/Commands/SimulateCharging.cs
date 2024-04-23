using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Messages.Commands;

/// <summary>
/// Provides a command to simulate agv charging.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="Level">The new battery level [%].</param>
public record SimulateCharging(Guid CorrelationId, double Level=50) : CorrelatedBy<Guid>;