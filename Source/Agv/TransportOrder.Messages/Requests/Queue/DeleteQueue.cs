namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;

/// <summary>
/// Provides a command to delete a queue.
/// </summary>
/// <param name="Id">The queue id.</param>
public record DeleteQueue(Guid Id);