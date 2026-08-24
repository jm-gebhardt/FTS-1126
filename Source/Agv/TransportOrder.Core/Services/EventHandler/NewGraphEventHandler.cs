using Gebhardt.Acc.Common.Messages.NewGraph;
using Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Reacts to changes in the new graph domain model.
/// </summary>
[UsedImplicitly]
public class NewGraphEventHandler(ILogger<NewGraphEventHandler> logger) : IConsumer<NewGraphReceived>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<NewGraphReceived> context)
    {
        var graph = context.Message.Graph;

        logger.LogDebug("Received a new graph with {NodeCount} nodes, {EdgeCount} edges and {StationCount} stations to save",
            graph.Nodes.Count, graph.Edges.Count, graph.Stations.Count);

        await context.Publish(new AddNewGraph(context.Message.CorrelationId, graph));
    }
}