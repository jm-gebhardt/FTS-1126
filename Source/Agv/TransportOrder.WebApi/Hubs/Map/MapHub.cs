using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Map;

/// <summary>
/// Hub for map/graph/node related notifications.
/// </summary>
[Authorize]
public class MapHub : Hub<IMapClient>
{
    /// <summary>
    /// Url path
    /// </summary>
    public const string UrlPath = "/hub/map";
}