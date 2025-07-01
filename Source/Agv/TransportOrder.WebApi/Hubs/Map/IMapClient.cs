using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;
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
}