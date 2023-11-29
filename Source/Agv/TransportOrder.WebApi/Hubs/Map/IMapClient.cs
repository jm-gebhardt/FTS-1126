using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Map;

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
}