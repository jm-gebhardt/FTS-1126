using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Queue;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Queue;

/// <summary>
/// Provides client methods for Queue-related events.
/// </summary>
[SignalRHub(QueueHub.UrlPath, documentNames: [HubApiGroup.GroupName])]
public interface IQueueClient
{
    /// <summary>
    /// Represents a change in the queue. More specifically, a change in the reservation or occupancy of its queue nodes.
    /// </summary>
    /// <param name="queue">The updated queue.</param>
    [SignalRMethod("QueueChanged")]
    Task QueueChanged(QueueDto queue);
}