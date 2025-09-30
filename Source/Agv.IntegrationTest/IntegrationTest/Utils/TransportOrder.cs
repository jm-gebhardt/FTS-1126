using Gebhardt.Acc.IntegrationTest.Api;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Polly;

namespace Gebhardt.Acc.IntegrationTest.Utils;

/// <summary>
/// Wrapper for TransportOrder client with methods to wait for something.
/// </summary>
[PublicAPI]
public class TransportOrder
{
    private readonly TransportOrderClient _client;
    private readonly ILogger _logger;

    /// <summary>
    /// TransportOrder constructor.
    /// </summary>
    public TransportOrder(TransportOrderClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Wait for a specific agv.
    /// </summary>
    /// <param name="agvSerialNumber"></param>
    /// <returns>Agv or null if not found or unsuccessful.</returns>
    public async Task<Api.Agv?> WaitForAgv(string agvSerialNumber)
    {
        return await Poll(async () =>
        {
            var agvs = await _client.AgvsGETAsync();
            if (agvs == null || agvs.Count == 0)
            {
                _logger.LogInformation("No agvs found");
                return null;
            }

            var agv = agvs.SingleOrDefault(agv => agv.SerialNumber == agvSerialNumber);
            if (agv == null)
            {
                _logger.LogInformation("Agv {AgvSerialNumber} not found", agvSerialNumber);
                return null;
            }

            _logger.LogInformation("Agv {AgvSerialNumber} found", agvSerialNumber);
            return agv;
        }, TimeSpan.FromMinutes(2));
    }

    /// <summary>
    /// Wait for a specific agv being idle.
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <param name="timeout"></param>
    /// <returns>Agv or null if not found or unsuccessful.</returns>
    public async Task<Api.Agv?> WaitForAgvIdle(Guid agvCorrelationId, TimeSpan timeout)
    {
        return await Poll(async () =>
        {
            var agv = await _client.AgvAsync(agvCorrelationId);

            if (agv == null)
            {
                _logger.LogInformation("Agv {AgvSerialNumber} not found", agvCorrelationId);
                return null;
            }

            if (agv.CurrentState != AgvStateType.Idle)
            {
                _logger.LogInformation("Agv {AgvSerialNumber} not idle yet. Current state: {CurrentState}", agvCorrelationId, agv.CurrentState);
                return null;
            }

            _logger.LogInformation("Agv {AgvSerialNumber} is idle", agvCorrelationId);
            return agv;
        }, timeout);
    }

    /// <summary>
    /// Wait for agv data.
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns>AgvData or null if not found or unsuccessful.</returns>
    public async Task<AgvData?> WaitForAgvData(Guid correlationId)
    {
        return await Poll(async () =>
        {
            var data = await _client.DataAsync(correlationId);
            if (data == null)
            {
                _logger.LogInformation("AgvData for agv {CorrelationId} not found", correlationId);
                return null;
            }

            _logger.LogInformation("AgvData for agv {CorrelationId} found", correlationId);
            return data;
        }, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Wait for graph.
    /// </summary>
    /// <returns>Graph or null if not found or unsuccessful.</returns>
    public async Task<Graph?> WaitForGraph(Guid mapCorrelationId)
    {
        return await Poll(async () =>
        {
            var graph = await _client.Graph2Async(mapCorrelationId);
            if (graph == null)
            {
                _logger.LogInformation("Graph for map '{MapCorrelationId}' not found", mapCorrelationId);
                return null;
            }

            _logger.LogInformation("Graph for map '{MapCorrelationId}' found", mapCorrelationId);
            return graph;
        }, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Wait for maps.
    /// </summary>
    /// <returns>Map or null if not found or unsuccessful.</returns>
    public async Task<ICollection<MapMetaData>?> WaitForMaps()
    {
        return await Poll(async () =>
        {
            var maps = await _client.MapsAsync();
            if (maps == null || maps.Count == 0)
            {
                _logger.LogInformation("No maps found");
                return null;
            }

            _logger.LogInformation("Maps found: {MapCount}", maps.Count);
            return maps;
        }, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Wait for map for agv.
    /// </summary>
    /// <param name="mapCorrelationId"></param>
    /// <param name="agvCorrelationId"></param>
    /// <returns>Map or null if not found or unsuccessful.</returns>
    public async Task<Map?> WaitForMap(Guid mapCorrelationId, Guid agvCorrelationId)
    {
        return await Poll(async () =>
        {
            var map = await _client.MapGETAsync(mapCorrelationId);
            if (map == null)
            {
                _logger.LogInformation("Map '{MapCorrelationId}' not found", mapCorrelationId);
                return null;
            }

            var relatedAgvs = map.RelatedAgvs;

            if (relatedAgvs.Any(agv => agv.ToString() == agvCorrelationId.ToString()))
            {
                _logger.LogInformation("Map '{MapCorrelationId}' related to agv '{AgvCorrelationId}'", mapCorrelationId, agvCorrelationId);
                return map;
            }

            _logger.LogInformation("Map '{MapCorrelationId}' not related to agv '{AgvCorrelationId}'", mapCorrelationId, agvCorrelationId);
            return null;
        }, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Wait for finished action group for order.
    /// </summary>
    /// <param name="actionGroupCorrelationId"></param>
    /// <returns>ActionGroup or null if not found or unsuccessful.</returns>
    public async Task<ActionGroup?> WaitForFinishedActionGroupForOrder(Guid actionGroupCorrelationId)
    {
        return await Poll(async () =>
        {
            var actionGroup = await _client.ActionGroupGETAsync(actionGroupCorrelationId);

            if (actionGroup == null)
            {
                _logger.LogInformation("Action group for order '{ActionGroupCorrelationId}' not found", actionGroupCorrelationId);
                return null;
            }

            var completedDate = actionGroup.CompletedDate;
            if (completedDate.ToString() == string.Empty)
            {
                _logger.LogInformation("Action group for order '{ActionGroupCorrelationId}' not finished yet", actionGroupCorrelationId);
                return null;
            }

            _logger.LogInformation("Action group for order '{ActionGroupCorrelationId}' finished at '{CompletedDate}'", actionGroupCorrelationId,
                completedDate);
            return actionGroup;
        }, TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Poll until request is successful.
    /// </summary>
    /// <param name="pollJob"></param>
    /// <param name="timeout"></param>
    /// <param name="delay"></param>
    /// <typeparam name="T"></typeparam>
    private async Task<T?> Poll<T>(Func<Task<T?>> pollJob, TimeSpan timeout, TimeSpan? delay = null) where T : class
    {
        var effectiveDelay = delay ?? TimeSpan.FromSeconds(1);
        var retries = (int) (timeout.TotalMilliseconds / effectiveDelay.TotalMilliseconds);

        var policy = Policy<T?>
            .HandleResult(r => r is null)
            .Or<Exception>()
            .WaitAndRetryAsync(
                retryCount: retries,
                sleepDurationProvider: _ => effectiveDelay,
                onRetry: (outcome, _, attempt, _) =>
                {
                    if (outcome.Exception != null)
                    {
                        _logger.LogError(outcome.Exception, "Poll exception at attempt {Attempt}", attempt);
                    }
                });

        try
        {
            return await policy.ExecuteAsync(pollJob);
        }
        catch (Exception)
        {
            _logger.LogError("Timeout in Poll after {Timeout}", timeout);
            return null;
        }
    }
}