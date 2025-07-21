using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.ActionGroup;

/// <summary>
/// Hub for ActionGroup related Dtos
/// </summary>
[Authorize]
public class ActionGroupHub : Hub<IActionGroupClient>
{
    /// <summary>
    /// Url path
    /// </summary>
    public const string UrlPath = "/hub/action";
}