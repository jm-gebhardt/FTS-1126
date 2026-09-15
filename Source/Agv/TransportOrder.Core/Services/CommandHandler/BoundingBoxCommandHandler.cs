using System.Diagnostics;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;
using Gebhardt.Acc.TransportOrder.Messages.Commands.BoundingBox;
using Gebhardt.Acc.TransportOrder.Messages.Events.BoundingBoxes;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Calculates and caches bounding box polygons for every known AGV type.
/// </summary>
[UsedImplicitly]
public class BoundingBoxCommandHandler : IConsumer<RecalculateBoundingBoxes>
{
    private readonly IBoundingBoxService _boundingBoxService;
    private readonly ILogger<BoundingBoxCommandHandler> _logger;

    /// <inheritdoc cref="BoundingBoxCommandHandler"/>
    public BoundingBoxCommandHandler(IBoundingBoxService boundingBoxService, ILogger<BoundingBoxCommandHandler> logger)
    {
        _boundingBoxService = boundingBoxService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<RecalculateBoundingBoxes> context)
    {
        _logger.LogInformation("Recalculating bounding boxes for all AGV types");
        var sw = Stopwatch.StartNew();
        await _boundingBoxService.RecalculateAllAsync();
        _logger.LogInformation("Recalculated bounding boxes for all AGV types in {ElapsedMilliseconds} ms", sw.ElapsedMilliseconds);
        await context.Publish(new BoundingBoxesRecalculated(context.Message.CorrelationId));
    }
}