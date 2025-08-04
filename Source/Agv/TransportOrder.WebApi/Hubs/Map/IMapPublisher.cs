using Gebhardt.Acc.Common.Domain.Models.Graphs;
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
    Task SendNodeStateChangedAsync(NodeLock nodeLock, Guid? stationId, string? modifyingUser, string? lockReason, string? lockDescription);

    /// <summary>
    /// Publishes node approaching property changes.
    /// </summary>
    /// <param name="mapId">The map id.</param>
    /// <param name="nodeId">The node id.</param>
    /// <param name="pickLiftHeight">The lift height for picking.</param>
    /// <param name="dropLiftHeight">The lift height for dropping.</param>
    /// <param name="dockingDirection">The docking direction.</param>
    Task SendNodeApproachingPropertiesUpdated(Guid mapId, string nodeId, double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection);

    /// <summary>
    /// Publishes jam state changes.
    /// </summary>
    /// <param name="stationId">The id of the station.</param>
    /// <param name="hasPalletJam">True if the station has a pallet jam.</param>
    Task SendJamStateUpdate(Guid stationId, bool hasPalletJam);

    /// <summary>
    /// Publishes map changes.
    /// </summary>
    Task SendMapChangedAsync(Messages.Requests.Map.Map map, bool hasBackgroundChanged);
}