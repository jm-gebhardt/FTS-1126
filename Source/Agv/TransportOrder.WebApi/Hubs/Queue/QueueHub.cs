using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Queue;

/// <summary>
/// Hub for Queue related Dtos
/// </summary>
[Authorize]
public class QueueHub : Hub<IQueueClient>
{
    /// <summary>
    /// Url path.
    /// </summary>
    public const string UrlPath = "/hub/queue";
}