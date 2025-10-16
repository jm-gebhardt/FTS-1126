using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.System;

/// <summary>
/// Hub for system related Dtos
/// </summary>
[Authorize]
public class SystemHub : Hub<ISystemClient>
{
    /// <summary>
    /// Url path
    /// </summary>
    public const string UrlPath = "/hub/system";
}