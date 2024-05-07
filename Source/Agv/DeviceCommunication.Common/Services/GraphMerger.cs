using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Common.Services;

/// <summary>
/// Gets the current (global) graph from the main system and merges it with the graph received from the Agv into a global Graph and publishes it back to the main system.
/// </summary>
[UsedImplicitly]
public class GraphMerger : IConsumer<AgvGraphReceived>
{
    private readonly IRequestClient<GetGraph> _getGraphClient;
    private readonly ILogger<GraphMerger> _logger;

    /// <inheritdoc cref="GraphMerger"/>
    /// <param name="getGraphClient">To get the current Graph.</param>
    /// <param name="logger"></param>
    public GraphMerger(IRequestClient<GetGraph> getGraphClient, ILogger<GraphMerger> logger)
    {
        _getGraphClient = getGraphClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AgvGraphReceived> context)
    {
        var correlationId = context.Message.CorrelationId;
        _logger.LogDebug("Received graph from Agv {AgvId} with correlationId {CorrelationId}", context.Message.SerialNumber, correlationId);
        Response<GotGraph> graphResponse = await _getGraphClient.GetResponse<GotGraph>(new GetGraph(correlationId));
        var graph = graphResponse.Message.Graph;

        if (graph.Merge(context.Message.Graph))
        {
            _logger.LogInformation("Merged graph from Agv {AgvId} with correlationId {CorrelationId}", context.Message.SerialNumber, correlationId);
            await context.Publish(new GraphChanged(correlationId, graph));
        }
        else
        {
            _logger.LogDebug("Graph from Agv {AgvId} with correlationId {CorrelationId} is already contained in the graph.", context.Message.SerialNumber, correlationId);
        }
    }
}