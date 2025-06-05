namespace Gebhardt.Acc.Common.Messages.Queue;

/// <summary>
/// Provides a command to evacuate a queues head.
/// </summary>
/// <param name="QueueName">The name of the queue.</param>
/// <param name="EvacuationTarget">The target node where to evacuate the agvs to.</param>
public record EvacuateQueueHead(string QueueName, string EvacuationTarget);