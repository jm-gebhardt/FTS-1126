namespace Gebhardt.Acc.Common.Messages.Queue;

/// <summary>
/// Provides a command to evacuate a queue.
/// </summary>
/// <param name="QueueName">The name of the queue.</param>
/// <param name="EvacuationTargetNodeId">The target node where to evacuate the agvs to.</param>
/// <param name="WaitingTime">The time [s] to wait in-between evacuation moves.</param>
public record EvacuateQueue(string QueueName, Guid EvacuationTargetNodeId, int WaitingTime = 3);