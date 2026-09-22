using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.TransportOrder.Messages.Commands.BoundingBox;
using Gebhardt.Acc.TransportOrder.Messages.Events.Graph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Publishes bounding box recalculation commands when the graph or its AGV type assignments change.
/// </summary>
[PublicAPI]
public class BoundingBoxEventHandler : IConsumer<GraphUpdated>, IConsumer<NewGraphUpdated>
{
    private readonly ILogger<BoundingBoxEventHandler> _logger;

    /// <inheritdoc cref="BoundingBoxEventHandler"/>
    public BoundingBoxEventHandler(ILogger<BoundingBoxEventHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GraphUpdated> context)
    {
        _logger.LogInformation("Graph updated. Triggering bounding box recalculation");
        await context.Publish(new RecalculateBoundingBoxes(context.Message.CorrelationId));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<NewGraphUpdated> context)
    {
        _logger.LogInformation("AGV type graphs changed. Triggering bounding box recalculation");
        await context.Publish(new RecalculateBoundingBoxes(Guid.Empty));
    }
}