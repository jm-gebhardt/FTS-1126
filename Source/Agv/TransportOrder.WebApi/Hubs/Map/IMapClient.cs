using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.LogEntry;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Map;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;

/// <summary>
/// Map related SignalR methods
/// </summary>
[SignalRHub(MapHub.UrlPath, documentNames: new[] { HubApiGroup.GroupName })]
public interface IMapClient
{
    /// <summary>
    /// Node state changed.
    /// </summary>
    /// <param name="nodeState"></param>
    [SignalRMethod("NodeStateChanged")]
    Task NodeStateChanged(NodeStateDto nodeState);

    /// <summary>
    /// Node approaching properties changed.
    /// </summary>
    [SignalRMethod("NodeApproachingPropertiesChanged")]
    Task NodeApproachingPropertiesChanged(NodeApproachingPropertiesDto nodeState);

    /// <summary>
    /// Station jam state changed.
    /// </summary>
    [SignalRMethod("StationJamStateChanged")]
    Task StationJamStateChanged(StationJamStateDto stationJamState);

    /// <summary>
    /// Map changed.
    /// </summary>
    [SignalRMethod("MapChanged")]
    Task MapChanged(MapChangedDto mapChangedDto);

    /// <summary>
    /// Publishes the last created station log to all clients.
    /// </summary>
    /// <param name="logEntry">The new log entry.</param>
    [SignalRMethod("StationLogCreated")]
    Task StationLogCreated(LogEntryCreatedDto logEntry);

    /// <summary>
    /// Station online state changed.
    /// </summary>
    /// <param name="dto">The station online state change data.</param>
    [SignalRMethod("StationIsOnlineChanged")]
    Task StationIsOnlineChanged(StationIsOnlineChangedDto dto);
}