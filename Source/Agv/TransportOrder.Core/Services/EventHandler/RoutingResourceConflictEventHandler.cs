using Gebhardt.Acc.TransportOrder.Messages.Commands.NewGraph;
using Gebhardt.Acc.TransportOrder.Messages.Events.BoundingBoxes;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Publishes conflict detection commands once the bounding boxes the detection relies on are up to date.
/// </summary>
[PublicAPI]
public class RoutingResourceConflictEventHandler : IConsumer<BoundingBoxesRecalculated>
{
    private readonly ILogger<RoutingResourceConflictEventHandler> _logger;

    /// <inheritdoc cref="RoutingResourceConflictEventHandler"/>
    public RoutingResourceConflictEventHandler(ILogger<RoutingResourceConflictEventHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<BoundingBoxesRecalculated> context)
    {
        _logger.LogInformation("Bounding boxes recalculated. Triggering conflict detection");
        await context.Publish(new DetectGraphConflicts(context.Message.CorrelationId));
    }
}