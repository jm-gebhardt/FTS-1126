using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Trigger;

/// <summary>
/// Hub for trigger related Dtos
/// </summary>
[Authorize]
public class TriggerHub : Hub<ITriggerClient>
{
    /// <summary>
    /// Url path
    /// </summary>
    public const string UrlPath = "/hub/trigger";
}