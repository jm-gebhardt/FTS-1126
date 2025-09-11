using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;

/// <summary>
/// Provides client methods for agv-related events.
/// </summary>
[SignalRHub(AgvHub.UrlPath, documentNames: new[] { HubApiGroup.GroupName })]
public interface IAgvClient
{
    /// <summary>
    /// Agv has been discovered.
    /// </summary>
    /// <param name="agv">The agv.</param>
    [SignalRMethod("AgvDiscovered")]
    Task AgvDiscovered(AgvDto agv);

    /// <summary>
    /// Agv state changed.
    /// </summary>
    /// <param name="agvState">The agv state.</param>
    [SignalRMethod("AgvStateChanged")]
    Task AgvStateChanged(AgvStateDto agvState);

    /// <summary>
    /// Agv mode changed.
    /// </summary>
    /// <param name="agvModeChanged">The agv mode.</param>
    [SignalRMethod("AgvModeChanged")]
    Task AgvModeChanged(AgvModeChangedDto agvModeChanged);

    /// <summary>
    /// Agv position changed.
    /// </summary>
    /// <param name="positionStatus">The agv position status.</param>
    [SignalRMethod("AgvPositionChanged")]
    Task AgvPositionChanged(AgvPositionStatusDto positionStatus);

    /// <summary>
    /// Agv errors changed.
    /// </summary>
    /// <param name="errorStatus">The agv error status.</param>
    [SignalRMethod("AgvErrorChanged")]
    Task AgvErrorChanged(AgvErrorStatusDto errorStatus);

    /// <summary>
    /// Agv online changed.
    /// </summary>
    /// <param name="onlineStatus">The agv online status.</param>
    [SignalRMethod("AgvOnlineChanged")]
    Task AgvOnlineChanged(AgvOnlineStatusDto onlineStatus);

    /// <summary>
    /// Agv locked changed.
    /// </summary>
    /// <param name="lockedStatus">The agv locked status.</param>
    [SignalRMethod("AgvLockedChanged")]
    Task AgvLockedChanged(AgvLockedStatusDto lockedStatus);

    /// <summary>
    /// Agv removed.
    /// </summary>
    /// <param name="removed">The agv removed status.</param>
    [SignalRMethod("AgvRemoved")]
    Task AgvRemoved(AgvRemovedDto removed);

    /// <summary>
    /// Agv route changed.
    /// </summary>
    /// <param name="currentRoute">The current route of the agv.</param>
    [SignalRMethod("AgvRouteChanged")]
    Task AgvRouteChanged(CurrentAgvRouteDto currentRoute);

    /// <summary>
    /// Assigned action group changed.
    /// </summary>
    /// <param name="assignedActionGroup"></param>
    [SignalRMethod("AssignedActionGroupChanged")]
    Task AssignedActionGroupChanged(AssignedActionGroupDto assignedActionGroup);

    /// <summary>
    /// Publishes the battery status of an Agv to all clients.
    /// </summary>
    /// <param name="batteryStatus">The agv battery status.</param>
    [SignalRMethod("BatteryStatusChanged")]
    Task BatteryStatusChanged(AgvBatteryStatusDto batteryStatus);

    /// <summary>
    /// Cargo of Agv(s) changed.
    /// </summary>
    /// <param name="cargos">The agv cargo.</param>
    [SignalRMethod("CargoChanged")]
    Task CargoChanged(ICollection<CargoDto> cargos);

    /// <summary>
    /// Agv hasCargo changed.
    /// </summary>
    /// <param name="hasCargoStatus">The agv cargo status.</param>
    [SignalRMethod("AgvHasCargoChanged")]
    Task AgvHasCargoChanged(AgvHasCargoDto hasCargoStatus);

    /// <summary>
    /// Agv zones changed.
    /// </summary>
    /// <param name="agvZones">The agv zones.</param>
    [SignalRMethod("AgvZonesChanged")]
    Task AgvZonesChanged(AgvZonesDto agvZones);

    /// <summary>
    /// Agv device internally reserved stations changed.
    /// </summary>
    /// <param name="agvStations">The agv stations.</param>
    [SignalRMethod("AgvStationsChanged")]
    Task AgvStationsChanged(AgvStationsDto agvStations);

    /// <summary>
    /// Publishes the operating data status of an Agv to all clients.
    /// </summary>
    /// <param name="agvOperatingDataStatus">The agv operating data status.</param>
    [SignalRMethod("AgvOperatingDataStatusChanged")]
    Task AgvOperatingDataStatusChanged(AgvOperatingDataStatusDto agvOperatingDataStatus);

    /// <summary>
    /// Publishes the last created maintenance log to all clients.
    /// </summary>
    /// <param name="maintenanceLogCreated">The created agv maintenance log.</param>
    [SignalRMethod("AgvMaintenanceLogCreated")]
    Task AgvMaintenanceLogCreated(MaintenanceLogCreatedDto maintenanceLogCreated);

    /// <summary>
    /// Publishes the last created maintenance log to all clients.
    /// </summary>
    /// <param name="maintenance">The agv maintenance.</param>
    [SignalRMethod("AgvMaintained")]
    Task AgvMaintained(AgvMaintainedDto maintenance);
}