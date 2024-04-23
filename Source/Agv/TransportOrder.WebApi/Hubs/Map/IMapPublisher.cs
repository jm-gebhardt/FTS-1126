using Gebhardt.Acc.TransportOrder.Domain.Models.Node;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;

/// <summary>
/// Represents the publisher of map/node/graph related notifications.
/// </summary>
public interface IMapPublisher
{
    /// <summary>
    /// Publishes a node state change.
    /// </summary>
    /// <param name="nodeState"></param>
    /// <returns></returns>
    Task SendNodeStateChangedAsync(NodeLock nodeState);
}