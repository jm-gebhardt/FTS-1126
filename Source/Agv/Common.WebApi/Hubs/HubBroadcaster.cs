using System.Collections.Concurrent;
using Gebhardt.Acc.Common.WebApi.Constants;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Common.WebApi.Hubs;

/// <inheritdoc/>
public class HubBroadcaster<THub, TClient> : IHubBroadcaster<THub, TClient>
    where THub : Hub<TClient>
    where TClient : class
{
    private readonly IHubContext<THub, TClient> _hubContext;
    private readonly IAbortableHubLifetimeManager<THub> _lifetimeManager;
    private readonly ILogger<HubBroadcaster<THub, TClient>> _logger;
    private readonly ConcurrentDictionary<string, List<CancellationTokenSource>> _ctsByConnectionId = new();

    /// <inheritdoc cref="HubBroadcaster{THub,TClient}"/>
    public HubBroadcaster(
        IHubContext<THub, TClient> hubContext,
        IAbortableHubLifetimeManager<THub> lifetimeManager,
        ILogger<HubBroadcaster<THub, TClient>> logger
    )
    {
        _hubContext = hubContext;
        _lifetimeManager = lifetimeManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Broadcast(Func<TClient, Task> invoke)
    {
        // Kick off the dispatch but don’t await it as some clients may have a bad connection which would block the caller.
        foreach (string connectionId in _lifetimeManager.GetAllConnectionIds())
        {
            _ = SendToClientAsync(connectionId, invoke);
        }
    }

    private async Task SendToClientAsync(string connectionId, Func<TClient, Task> invoke)
    {
        if (!_ctsByConnectionId.TryGetValue(connectionId, out List<CancellationTokenSource>? ctsList))
        {
            ctsList = [];
            _ctsByConnectionId[connectionId] = ctsList;
        }
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(WebApiConstants.HubInvocationTimeoutSeconds));
        ctsList.Add(cts);
        var currentCts = cts;

        try
        {
            await invoke(_hubContext.Clients.Client(connectionId))
                .WaitAsync(cts.Token);
        }
        catch (Exception ex)
            when (ex is TaskCanceledException or OperationCanceledException)
        {
            if (_lifetimeManager.AbortConnection(connectionId))
            {
                _logger.LogError(ex, "Failed to send to client {ConnectionId}, evicted connection", connectionId);
            }
            _logger.LogWarning(ex, "Failed to send to client {ConnectionId}, connection timed out", connectionId);

            await Task.WhenAll(_ctsByConnectionId[connectionId]
                .Where(cts2 => cts2 != currentCts)
                .Select(cts2 => cts2.CancelAsync()));
        }
        finally
        {
            ctsList.Remove(cts);
            if (ctsList.Count == 0)
            {
                _ctsByConnectionId.TryRemove(connectionId, out _);
            }
        }
    }
}