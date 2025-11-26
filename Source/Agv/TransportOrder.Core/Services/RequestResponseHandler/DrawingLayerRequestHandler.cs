using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Drawing;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles drawing layer related requests.
/// </summary>
[UsedImplicitly]
public class DrawingLayerRequestHandler :
    IConsumer<GetDrawingLayer>,
    IConsumer<SetDrawingLayer>,
    IConsumer<DeleteDrawingLayer>
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
            drawingLayer?.ElementsJson,
            drawingLayer?.LastModified
        ));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<SetDrawingLayer> context)
    {
        var message = context.Message;
        _logger.LogInformation("Setting drawing layer for map {CorrelationId}", message.CorrelationId);

        var drawingLayer = new MapDrawingLayer(message.CorrelationId, message.ElementsJson);

        await _mapRepository.SetDrawingLayerAsync(message.CorrelationId, drawingLayer);

        _logger.LogInformation("Drawing layer set successfully for map {CorrelationId}", message.CorrelationId);

        await context.RespondAsync(new Success());
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DeleteDrawingLayer> context)
    {
        var correlationId = context.Message.CorrelationId;
        _logger.LogInformation("Deleting drawing layer for map {CorrelationId}", correlationId);

        var deleted = await _mapRepository.DeleteDrawingLayerAsync(correlationId);

        if (deleted)
        {
            _logger.LogInformation("Drawing layer deleted for map {CorrelationId}", correlationId);
        }
        else
        {
            _logger.LogInformation("No drawing layer to delete for map {CorrelationId}", correlationId);
        }

        await context.RespondAsync(new Success());
    }
}
