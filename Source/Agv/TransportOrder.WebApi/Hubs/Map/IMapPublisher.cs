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
    /// <param name="nodeLock">NodeLock containing new node state</param>
    /// <param name="stationId">Station associated with node lock if applicable.</param>
    /// <param name="modifyingUser">User that modified station if applicable.</param>
    /// <param name="lockReason">Reason station was locked if applicable.</param>
    /// <param name="lockDescription">Description of lock reason of station if applicable.</param>
    /// <returns></returns>
    Task SendNodeStateChangedAsync(NodeLock nodeLock, Guid? stationId, string? modifyingUser, string? lockReason, string? lockDescription);
}