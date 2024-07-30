using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands;

/// <summary>
/// Provides a command to handle an agv passing the short loop trigger point.
/// </summary>
/// <param name="Agv">The agv.</param>
/// <param name="CorrelationId">The correlation id of the assigned order.</param>
public record PassShortLoopTriggerPoint(Guid Agv, Guid CorrelationId): CorrelatedBy<Guid>;