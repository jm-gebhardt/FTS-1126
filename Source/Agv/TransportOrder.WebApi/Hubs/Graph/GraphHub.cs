using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Graph;

/// <summary>
/// Hub for graph and view related notifications.
/// </summary>
[Authorize]
public class GraphHub : Hub<IGraphClient>
{
    /// <summary>
    /// Url path
    /// </summary>
    public const string UrlPath = "/hub/graph";
}