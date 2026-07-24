using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
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
    private readonly IBoundingBoxRepository _boundingBoxRepository;
    private readonly IMapper _mapper;

    /// <inheritdoc cref="BoundingBoxRequestHandler"/>
    public BoundingBoxRequestHandler(IBoundingBoxRepository boundingBoxRepository, IMapper mapper)
    {
        _boundingBoxRepository = boundingBoxRepository;
        _mapper = mapper;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetBoundingBoxes> context)
    {
        IReadOnlyList<ResourceBoundingBox> boxes;
        if (context.Message.ResourceId.HasValue)
        {
            var box = await _boundingBoxRepository.GetAsync(context.Message.ResourceId.Value, context.Message.AgvType, context.CancellationToken);
            boxes = box != null ? [box] : [];
        }
        else
        {
            boxes = await _boundingBoxRepository.GetByAgvTypeAsync(context.Message.AgvType, context.CancellationToken);
        }

        var content = _mapper.Map<List<ResourceBoundingBoxContent>>(boxes);
        await context.RespondAsync(new GotBoundingBoxes(content));
    }
}