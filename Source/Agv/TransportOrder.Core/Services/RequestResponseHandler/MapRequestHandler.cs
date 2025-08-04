using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map.Image;
using JetBrains.Annotations;
using MassTransit;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for map related requests.
/// </summary>
[UsedImplicitly]
public class MapRequestHandler :
    IConsumer<GetMapList>,
    IConsumer<GetMap>,
    IConsumer<GetBackgroundImage>,
    IConsumer<TrySettingNodeApproachingProperties>
{
    private readonly IMapRepository _mapRepository;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IGraphRepository _graphRepository;

    /// <inheritdoc cref="MapRequestHandler"/>/>
    public MapRequestHandler(IMapRepository mapRepository,
        IMapper mapper,
        IPublishEndpoint publishEndpoint,
        IGraphRepository graphRepository
    )
    {
        _mapRepository = mapRepository;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
        _graphRepository = graphRepository;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetMapList> context)
    {
        ICollection<Map> fetchedMaps = await _mapRepository.GetAllMapsAsync();
        // reversing the list is a quick fix that the global map is on that. This needs improvement in the future.
        var maps = _mapper.Map<List<Messages.Requests.Map.Map>>(fetchedMaps.Reverse());
        await context.RespondAsync(new GotMapList { Maps = maps });
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetMap> context)
    {
        var correlationId = context.Message.CorrelationId;
        var fetchedMap = await _mapRepository.GetMapAsync(correlationId);
        var map = _mapper.Map<Messages.Requests.Map.Map>(fetchedMap);
        await context.RespondAsync(new GotMap(map));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetBackgroundImage> context)
    {
        var correlationId = context.Message.CorrelationId;

        byte[]? image = await _mapRepository.GetBackgroundImageAsync(correlationId);
        if (image == null || image.Length == 0)
        {
            throw new MapNotFoundException(correlationId);
        }

        await context.RespondAsync(new GotBackgroundImage(image));
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<TrySettingNodeApproachingProperties> context)
    {
        (var correlationId, string nodeId, double? pickLiftHeight, double? dropLiftHeight, DockingDirection? dockingDirection) = context.Message;
        await _mapRepository.GetMapAsync(correlationId);
        await _graphRepository.GetNodeAsync(nodeId);

        await _publishEndpoint.Publish(new SetNodeApproachingProperties(correlationId, nodeId, pickLiftHeight, dropLiftHeight, dockingDirection));

        await context.RespondAsync(new Success());
    }
}