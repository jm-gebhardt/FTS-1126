using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
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
    /// <param name="agv"></param>
    [SignalRMethod("AgvChanged")]
    Task AgvChanged(AgvDto agv);

    /// <summary>
    /// AGV position changed
    /// </summary>
    /// <param name="position"></param>
    [SignalRMethod("AgvPositionChanged")]
    Task AgvPositionChanged(AgvPositionStatusDto position);

    /// <summary>
    /// AGV route changed
    /// </summary>
    /// <param name="route"></param>
    /// <returns></returns>
    [SignalRMethod("AgvRouteChanged")]
    Task AgvRouteChanged(AgvRouteDto route);
}