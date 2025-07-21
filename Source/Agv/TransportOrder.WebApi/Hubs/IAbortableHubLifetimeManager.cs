using Microsoft.AspNetCore.SignalR;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs;

/// <summary>
/// Interface for managing SignalR hub connections with support for aborting client connections.
/// </summary>
/// <typeparam name="THub">Type of the SignalR Hub.</typeparam>
public interface IAbortableHubLifetimeManager<THub> where THub : Hub
{
    /// <summary>
    /// Gets all connection IDs
    /// </summary>
    IEnumerable<string> GetAllConnectionIds();

    /// <summary>
    /// Terminates the connection.
    /// </summary>
    /// <returns>True if any connection was aborted, false otherwise.</returns>
    bool AbortConnection(string connectionId);
}