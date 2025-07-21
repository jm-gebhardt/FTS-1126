using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;

/// <summary>
/// Hub for Agv related Dtos
/// </summary>
[Authorize]
public class AgvHub : Hub<IAgvClient>
{
    /// <summary>
    /// Url path
    /// </summary>
    public const string UrlPath = "/hub/agv";
}