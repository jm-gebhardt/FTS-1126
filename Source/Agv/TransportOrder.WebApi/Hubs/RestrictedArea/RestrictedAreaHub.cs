using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.RestrictedArea;

/// <summary>
/// Hub for restricted area related messages.
/// </summary>
[Authorize]
public class RestrictedAreaHub : Hub<IRestrictedAreaClient>
{
    /// <summary>
    /// Url path
    /// </summary>
    public const string UrlPath = "/hub/restrictedArea";
}