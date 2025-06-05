using Gebhardt.Acc.Common.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Represents the exception thrown when a queue could not be found.
/// </summary>
public class QueueNotFoundException: NotFoundException
{
    /// <inheritdoc cref="QueueNotFoundException"/>>
    public QueueNotFoundException(Guid id) : base("Queue", nameof(id), id){}

    /// <inheritdoc cref="QueueNotFoundException"/>>
    public QueueNotFoundException(string name) : base("Queue", nameof(name), name) { }
}