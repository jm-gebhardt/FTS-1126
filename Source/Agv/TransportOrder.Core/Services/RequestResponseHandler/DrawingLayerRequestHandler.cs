using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Drawing;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles drawing layer read requests.
/// Write operations (SetDrawingLayer, DeleteDrawingLayer) are handled by MapCommandHandler
/// with partitioning for synchronization with other map write operations.
/// </summary>
[UsedImplicitly]
public class DrawingLayerRequestHandler :
    IConsumer<GetDrawingLayer>
{
    private readonly IMapRepository _mapRepository;
    private readonly ILogger<DrawingLayerRequestHandler> _logger;

    /// <inheritdoc cref="DrawingLayerRequestHandler"/>
    public DrawingLayerRequestHandler(
        IMapRepository mapRepository,
        ILogger<DrawingLayerRequestHandler> logger)
    {
        _mapRepository = mapRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetDrawingLayer> context)
    {
        var correlationId = context.Message.CorrelationId;
        _logger.LogDebug("Getting drawing layer for map {CorrelationId}", correlationId);

        var drawingLayer = await _mapRepository.GetDrawingLayerAsync(correlationId);

        await context.RespondAsync(new GotDrawingLayer(
            drawingLayer?.MapCorrelationId,
            drawingLayer?.Version,
            drawingLayer?.Elements,
            drawingLayer?.LastModified
        ));
    }
}
