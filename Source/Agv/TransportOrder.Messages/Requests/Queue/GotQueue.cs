namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;

/// <summary>
/// Represents the response to single queue related requests.
/// </summary>
/// <param name="Queue">The queue.</param>
public record GotQueue(Domain.Models.Queue.Queue Queue);