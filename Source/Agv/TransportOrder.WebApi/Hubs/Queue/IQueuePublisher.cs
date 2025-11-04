namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Queue;

/// <summary>
/// Publishes Queue related events.
/// </summary>
public interface IQueuePublisher
{
    /// <summary>
    /// Publishes the update of a queue to all clients.
    /// </summary>
    Task SendQueueChangedAsync(Common.Messages.Queue.Queue queue);
}