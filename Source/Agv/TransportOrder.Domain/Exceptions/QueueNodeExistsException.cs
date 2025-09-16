namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception thrown when the given node is already in the queue.
/// </summary>
public class QueueNodeExistsException : Exception
{
    /// <inheritdoc cref="QueueNodeExistsException"/>>
    public QueueNodeExistsException(Guid nodeId) : base($"{nodeId} is already part of the queue")
    {
    }

    /// <inheritdoc cref="QueueNodeExistsException"/>>
    public QueueNodeExistsException(int position) : base($"There is already a node at position {position} of the queue")
    {
    }
}