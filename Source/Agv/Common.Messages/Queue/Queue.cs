using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Queue;

/// <summary>
/// A queue.
/// </summary>
/// <param name="CorrelationId">The Id of the queue.</param>
/// <param name="Name">The name of the queue.</param>
/// <param name="QueueNodes">The nodes of the queue.</param>
[UsedImplicitly]
public record Queue(Guid CorrelationId, string Name, List<QueueNode> QueueNodes);