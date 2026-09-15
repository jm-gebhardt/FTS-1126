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
public class NewGraphEventHandler(ILogger<NewGraphEventHandler> logger) : IConsumer<NewGraphSegmentReceived>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<NewGraphSegmentReceived> context)
    {
        var graphSegment = context.Message.GraphSegment;

        logger.LogDebug("Received a new graph segment with {NodeCount} nodes, {EdgeCount} edges and {StationCount} stations to save",
            graphSegment.Nodes.Count, graphSegment.Edges.Count, graphSegment.Stations.Count);

        await context.Publish(new AddNewGraphSegment(context.Message.CorrelationId, graphSegment));
    }
}