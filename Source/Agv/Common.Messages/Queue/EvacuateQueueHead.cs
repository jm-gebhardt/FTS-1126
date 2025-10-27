using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Queue;

/// <summary>
/// Provides a command to evacuate a queues head.
/// </summary>
/// <param name="QueueName">The name of the queue.</param>
/// <param name="EvacuationTargetId">The target node where to evacuate the agvs to.</param>
/// <param name="TriggeringAgv">The agv causing the evacuation.</param>
public record EvacuateQueueHead(string QueueName, Guid EvacuationTargetId, Guid TriggeringAgv) : CorrelatedBy<string>
{
    /// <inheritdoc />
    public string CorrelationId => QueueName;
}