namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Queue;

/// <summary>
/// Represents the response to <see cref="GetQueues"/>.
/// </summary>
/// <param name="Queues">The queues.</param>
public record GotQueues(ICollection<Domain.Models.Queue.Queue> Queues);