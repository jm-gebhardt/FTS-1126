using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Common.Services;

/// <summary>
/// Gets the current (global) graph from the main system and merges it with the graph received from the Agv into a global Graph and publishes it back to the main system.
/// </summary>
[UsedImplicitly]
public class GraphMerger : IConsumer<InternalAgvGraphReceived>
{
    private readonly IRequestClient<GetGraph> _getGraphClient;
    private readonly IRequestClient<UpdateGraphRequest> _updateGraphClient;
    private readonly ILogger<GraphMerger> _logger;

    /// <inheritdoc cref="GraphMerger"/>
    public GraphMerger(IRequestClient<GetGraph> getGraphClient,
        IRequestClient<UpdateGraphRequest> updateGraphClient,
        ILogger<GraphMerger> logger)
    {
        _getGraphClient = getGraphClient;
        _updateGraphClient = updateGraphClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<InternalAgvGraphReceived> context)
    {
        var correlationId = context.Message.CorrelationId;
        _logger.LogInformation("Received graph from Agv {AgvId} with correlationId {CorrelationId}", context.Message.SerialNumber, correlationId);
        var graphResponse = await _getGraphClient.GetResponse<GotGraph>(new GetGraph(correlationId));
        var graph = graphResponse.Message.Graph;

        if (graph.Merge(context.Message.Graph))
        {
            _logger.LogInformation("Merged graph from Agv {AgvId} with correlationId {CorrelationId}", context.Message.SerialNumber, correlationId);
            await _updateGraphClient.GetResponse<Success>(new UpdateGraphRequest(correlationId, graph));
        }
        else
        {
            _logger.LogInformation("Graph from Agv {AgvId} with correlationId {CorrelationId} is already contained in the graph", context.Message.SerialNumber, correlationId);
        }

        await context.Publish(new AgvGraphReceived(correlationId, context.Message.SerialNumber, context.Message.Graph), context.CancellationToken);
    }
}