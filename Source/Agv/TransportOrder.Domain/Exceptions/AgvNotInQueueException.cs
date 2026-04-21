using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// The AGV is not registered in the specified queue.
/// </summary>
public class AgvNotInQueueException : ValidationException
{
    /// <inheritdoc cref="AgvNotInQueueException"/>>
    public AgvNotInQueueException(Guid agvCorrelationId, Guid? queueId)
        : base($"Agv with CorrelationId {agvCorrelationId} is not in {(queueId.HasValue ? $"Queue with Id {queueId}" : "any Queue")}.")
    {
    }
}