using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Graph;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.ActionGroup;
using SignalRSwaggerGen.Attributes;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Map;

/// <summary>
/// Map related SignalR methods
/// </summary>
[SignalRHub(ActionGroupHub.UrlPath, documentNames: new[] { HubApiGroup.GroupName })]
public interface IMapClient
{
    /// <summary>
    /// Node state changed.
    /// </summary>
    /// <param name="nodeState"></param>
    [SignalRMethod("NodeStateChanged")]
    Task NodeStateChanged(NodeStateDto nodeState);
}