using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles bounding box requests.
/// </summary>
[UsedImplicitly]
public class BoundingBoxRequestHandler : IConsumer<GetBoundingBoxes>
{
    private readonly IBoundingBoxService _boundingBoxService;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="BoundingBoxRequestHandler"/>
    public BoundingBoxRequestHandler(IBoundingBoxService boundingBoxService, IMapper mapper)
    {
        _boundingBoxService = boundingBoxService;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetBoundingBoxes> context)
    {
        IReadOnlyList<ResourceBoundingBox> boxes;
        if (context.Message.ResourceId.HasValue)
        {
            var box = await _boundingBoxService.GetAsync(context.Message.ResourceId.Value, context.Message.AgvType);
            boxes = box != null ? [box] : [];
        }
        else
        {
            boxes = await _boundingBoxService.GetByAgvTypeAsync(context.Message.AgvType);
        }

        var content = _mapper.Map<List<ResourceBoundingBoxContent>>(boxes);
        await context.RespondAsync(new GotBoundingBoxes(content));
    }
}