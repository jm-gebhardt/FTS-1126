using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Commands;

/// <summary>
/// Provides a command to simulate failure of the current action of the given agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
public record SimulateActionFailure(Guid CorrelationId) : CorrelatedBy<Guid>;

