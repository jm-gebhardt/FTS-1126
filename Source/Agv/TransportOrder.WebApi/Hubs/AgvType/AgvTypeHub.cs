using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.AgvType;

/// <summary>
/// Hub for agv type related messages
/// </summary>
[Authorize]
public class AgvTypeHub : Hub<IAgvTypeClient>
{
    /// <summary>
    /// Url path
    /// </summary>
    public const string UrlPath = "/hub/agvType";
}