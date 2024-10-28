namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;

/// <summary>
/// Provides a request to get a queue.
/// </summary>
/// <param name="Id">The id of the queue.</param>
public record GetQueue(Guid Id);