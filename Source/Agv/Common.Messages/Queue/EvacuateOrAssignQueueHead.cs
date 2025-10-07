using MassTransit;

namespace Gebhardt.Acc.Common.Messages.Queue;

/// <summary>
/// Provides a command to evacuate a queues head or assign pick order to agv.
/// </summary>
/// <param name="QueueName">The name of the queue.</param>
/// <param name="EvacuationTargetId">The target node where to evacuate the agvs to.</param>
/// <param name="TriggeringAgv">The agv that triggered the evacuation.</param>
/// <param name="StationName">The target station to consider for pick assignment.</param>
public record EvacuateOrAssignQueueHead(string QueueName, Guid EvacuationTargetId, Guid TriggeringAgv, string StationName) : CorrelatedBy<string>
{
    /// <inheritdoc cref="CorrelatedBy{TKey}"/>>
    public string CorrelationId => QueueName;
}