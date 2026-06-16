using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.TransportOrder.Messages.Commands.BoundingBox;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Publishes bounding box calculation commands when the graph or AGV fleet changes.
/// </summary>
[PublicAPI]
public class BoundingBoxEventHandler : IConsumer<GraphUpdated>, IConsumer<AgvDataReceived>
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
        await context.Publish(new CalculateBoundingBoxes(context.Message.CorrelationId, context.Message.Graph));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AgvDataReceived> context)
    {
        _logger.LogInformation("AGV data received for type \"{AgvType}\". Triggering bounding box calculation", context.Message.Data.Type);
        await context.Publish(new CalculateBoundingBoxesForAgvType(context.Message.CorrelationId, context.Message.Data));
    }
}