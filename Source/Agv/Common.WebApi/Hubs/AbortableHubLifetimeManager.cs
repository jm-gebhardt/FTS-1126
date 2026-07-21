using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.WebApi.Hubs;

/// <summary>
/// A hub lifetime manager that supports explicitly aborting client connections.
/// </summary>
/// <typeparam name="THub">Type of the SignalR Hub.</typeparam>
public class AbortableHubLifetimeManager<THub> : DefaultHubLifetimeManager<THub>, IAbortableHubLifetimeManager<THub> where THub : Hub
{
    private readonly ILogger<AbortableHubLifetimeManager<THub>> _logger;
    private readonly ConcurrentDictionary<string, HubConnectionContext> _connections = new();

    /// <inheritdoc />
    public AbortableHubLifetimeManager(ILogger<AbortableHubLifetimeManager<THub>> logger) : base(logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public override Task OnConnectedAsync(HubConnectionContext connection)
    {
        _connections[connection.ConnectionId] = connection;
        _logger.LogInformation("Client connected. Connection ID: {ConnectionId}", connection.ConnectionId);
        return base.OnConnectedAsync(connection);
    }

    /// <inheritdoc />
    public override Task OnDisconnectedAsync(HubConnectionContext connection)
    {
        _connections.TryRemove(connection.ConnectionId, out _);
        _logger.LogInformation("Client disconnected. Connection ID: {ConnectionId}", connection.ConnectionId);

        return base.OnDisconnectedAsync(connection);
    }

    /// <summary>
    /// Gets all connection IDs
    /// </summary>
    public IEnumerable<string> GetAllConnectionIds()
    {
        return _connections.Keys;
    }

    /// <summary>
    /// Abruptly terminates the transport for the given connection.
    /// </summary>
    public bool AbortConnection(string connectionId)
    {
        if (!_connections.TryRemove(connectionId, out var ctx))
        {
            return false;
        }

        _logger.LogWarning("Aborting connection {ConnectionId}", connectionId);

        ctx.Abort();
        return true;
    }
}