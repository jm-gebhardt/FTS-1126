using Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;
using JetBrains.Annotations;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Graph;

/// <summary>
/// Provides client methods for graph and view related events.
/// </summary>
[SignalRHub(GraphHub.UrlPath, documentNames: [HubApiGroup.GroupName])]
public interface IGraphClient
{
    /// <summary>
    /// The graph has changed.
    /// </summary>
    [SignalRMethod(nameof(GraphChanged))]
    Task GraphChanged();

    /// <summary>
    /// A view has been created or changed.
    /// </summary>
    /// <param name="view">The created or changed view.</param>
    [SignalRMethod(nameof(ViewChanged))]
    Task ViewChanged(ViewDto view);

    /// <summary>
    /// A view has been deleted.
    /// </summary>
    /// <param name="viewId">The id of the deleted view.</param>
    [SignalRMethod(nameof(ViewDeleted))]
    Task ViewDeleted(Guid viewId);

    /// <summary>
    /// The runtime state of a node has changed.
    /// </summary>
    /// <param name="nodeId">The id of the node.</param>
    /// <param name="status">The new runtime state of the node.</param>
    [UsedImplicitly]
    [SignalRMethod(nameof(NodeStatusChanged))]
    Task NodeStatusChanged(Guid nodeId, NodeStatusDto status);

    /// <summary>
    /// The state of a station has changed.
    /// </summary>
    /// <param name="stationId">The id of the station.</param>
    /// <param name="status">The new state of the station.</param>
    [UsedImplicitly]
    [SignalRMethod(nameof(StationStatusChanged))]
    Task StationStatusChanged(Guid stationId, StationStatusDto status);
}