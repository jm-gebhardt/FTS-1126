using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Queue;

/// <summary>
/// Provides a command to evacuate a queue.
/// </summary>
/// <param name="QueueName">The name of the queue.</param>
/// <param name="EvacuationTargetNodeId">The target node where to evacuate the agvs to.</param>
/// <param name="TriggeringAgv">The agv causing the evacuation.</param>
/// <param name="WaitingTime">The time [s] to wait in-between evacuation moves.</param>
public record EvacuateQueue(string QueueName, Guid EvacuationTargetNodeId, Guid TriggeringAgv, int WaitingTime = 3) : CorrelatedBy<string>
{
    /// <inheritdoc />
    public string CorrelationId => QueueName;
}