using Gebhardt.StoreWare.Agv.Common.Messages.Graphs.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Graphs;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Consumer for <see cref="GraphChanged"/>.
/// </summary>
[UsedImplicitly]
public class GraphEventHandler : IConsumer<GraphChanged>
{
    private readonly ILogger<GraphEventHandler> _logger;

    /// <inheritdoc cref="GraphEventHandler"/>
    /// <param name="logger"></param>
    public GraphEventHandler(ILogger<GraphEventHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="GraphChanged"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<GraphChanged> context)
    {
        _logger.LogInformation("{MessageType}", nameof(GraphChanged));
        await context.Publish(new UpdateGraph(context.Message.CorrelationId, context.Message.Graph, context.Message.Maps));
    }
}