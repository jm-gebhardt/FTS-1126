using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.Common.WebApi.Hubs;

/// <summary>
/// Provides methods to broadcast messages to all connected clients of a SignalR hub without awaiting or blocking.
/// </summary>
/// <typeparam name="THub">Type of SignalR Hub.</typeparam>
/// <typeparam name="TClient">Interface representing client methods.</typeparam>
public interface IHubBroadcaster<THub, out TClient>
    where THub : Hub
    where TClient : class
{
    /// <summary>
    /// Fire-and-forget broadcast to all clients.
    /// </summary>
    void Broadcast(Func<TClient, Task> clientFunc);
}