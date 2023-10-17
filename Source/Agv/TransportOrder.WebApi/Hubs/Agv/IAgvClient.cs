using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.AgvActions;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;

/// <summary>
/// Agv related SignalR methods
/// </summary>
[SignalRHub(path: AgvHub.UrlPath, documentNames: new[] {HubApiGroup.GroupName})]
public interface IAgvClient
{
    /// <summary>
    /// AGV has been discovered
    /// </summary>
    /// <param name="agv"></param>
    [SignalRMethod("AgvDiscovered")]
    Task AgvDiscovered(AgvDto agv);

    /// <summary>
    /// AGV state changed
    /// </summary>
    /// <param name="agvState"></param>
    [SignalRMethod("AgvStateChanged")]
    Task AgvStateChanged(AgvStateDto agvState);

    /// <summary>
    /// AGV position changed
    /// </summary>
    /// <param name="positionStatus"></param>
    [SignalRMethod("AgvPositionChanged")]
    Task AgvPositionChanged(AgvPositionStatusDto positionStatus);

    /// <summary>
    /// AGV errors changed
    /// </summary>
    /// <param name="errorStatus"></param>
    [SignalRMethod("AgvErrorChanged")]
    Task AgvErrorChanged(AgvErrorStatusDto errorStatus);

    /// <summary>
    /// AGV online changed
    /// </summary>
    /// <param name="onlineStatus"></param>
    [SignalRMethod("AgvOnlineChanged")]
    Task AgvOnlineChanged(AgvOnlineStatusDto onlineStatus);

    /// <summary>
    /// AGV locked changed
    /// </summary>
    /// <param name="lockedStatus"></param>
    [SignalRMethod("AgvLockedChanged")]
    Task AgvLockedChanged(AgvLockedStatusDto lockedStatus);

    /// <summary>
    /// AGV removed
    /// </summary>
    /// <param name="removed"></param>
    [SignalRMethod("AgvRemoved")]
    Task AgvRemoved(AgvRemovedDto removed);

    /// <summary>
    /// AGV route changed
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
    /// Publishes the battery status of an agv to all clients
    /// </summary>
    /// <param name="batteryStatus"></param>
    /// <returns></returns>
    [SignalRMethod("BatteryStatusChanged")]
    Task SendBatteryStatus(AgvBatteryStatusDto batteryStatus);
}