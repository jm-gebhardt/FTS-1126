using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;

/// <summary>
/// Agv related SignalR methods
/// </summary>
[SignalRHub(AgvHub.UrlPath, documentNames: new[] { HubApiGroup.GroupName })]
public interface IAgvClient
{
    /// <summary>
    /// Agv has been discovered
    /// </summary>
    /// <param name="agv"></param>
    [SignalRMethod("AgvDiscovered")]
    Task AgvDiscovered(AgvDto agv);

    /// <summary>
    /// Agv state changed
    /// </summary>
    /// <param name="agvState"></param>
    [SignalRMethod("AgvStateChanged")]
    Task AgvStateChanged(AgvStateDto agvState);

    /// <summary>
    /// Agv position changed
    /// </summary>
    /// <param name="positionStatus"></param>
    [SignalRMethod("AgvPositionChanged")]
    Task AgvPositionChanged(AgvPositionStatusDto positionStatus);

    /// <summary>
    /// Agv errors changed
    /// </summary>
    /// <param name="errorStatus"></param>
    [SignalRMethod("AgvErrorChanged")]
    Task AgvErrorChanged(AgvErrorStatusDto errorStatus);

    /// <summary>
    /// Agv online changed
    /// </summary>
    /// <param name="onlineStatus"></param>
    [SignalRMethod("AgvOnlineChanged")]
    Task AgvOnlineChanged(AgvOnlineStatusDto onlineStatus);

    /// <summary>
    /// Agv locked changed
    /// </summary>
    /// <param name="lockedStatus"></param>
    [SignalRMethod("AgvLockedChanged")]
    Task AgvLockedChanged(AgvLockedStatusDto lockedStatus);

    /// <summary>
    /// Agv removed
    /// </summary>
    /// <param name="removed"></param>
    [SignalRMethod("AgvRemoved")]
    Task AgvRemoved(AgvRemovedDto removed);

    /// <summary>
    /// Agv route changed
    /// </summary>
    /// <param name="currentRoute"></param>
    /// <returns></returns>
    [SignalRMethod("AgvRouteChanged")]
    Task AgvRouteChanged(CurrentAgvRouteDto currentRoute);

    /// <summary>
    /// Assigned action group changed
    /// </summary>
    /// <param name="assignedActionGroup"></param>
    [SignalRMethod("AssignedActionGroupChanged")]
    Task AssignedActionGroupChanged(AssignedActionGroupDto assignedActionGroup);

    /// <summary>
    /// Publishes the battery status of an Agv to all clients
    /// </summary>
    /// <param name="batteryStatus"></param>
    /// <returns></returns>
    [SignalRMethod("BatteryStatusChanged")]
    Task SendBatteryStatus(AgvBatteryStatusDto batteryStatus);
    
    /// <summary>
    /// Cargo of Agv(s) changed
    /// </summary>
    /// <param name="cargos"></param>
    /// <returns></returns>
    [SignalRMethod("CargoChanged")]
    Task AgvCargoChanged(ICollection<CargoDto> cargos);
}