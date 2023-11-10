using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Map;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides map-related command handlers.
/// </summary>
[UsedImplicitly]
public class MapCommandHandler : IConsumer<SaveMap>
{
    private readonly IMapRepository _mapRepository;
    private readonly ILogger<MapCommandHandler> _logger;

    /// <summary>
    /// Initializes a new <see cref="MapCommandHandler"/>.
    /// </summary>
    public MapCommandHandler(IMapRepository mapRepository, ILogger<MapCommandHandler> logger)
    {
        _mapRepository = mapRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="SaveMap"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SaveMap> context)
    {
        var (agvMap, mapId) = context.Message;
        var cancellationToken = context.CancellationToken;

        byte[] mapValue = Convert.FromBase64String(agvMap.ImageData);

        var map = new Map(
            mapId,
            agvMap.MapId,
            mapValue,
            agvMap.Width,
            agvMap.Height,
            agvMap.Resolution,
            agvMap.XOffset,
            agvMap.YOffset,
            agvMap.ModificationDate
        );

        bool mapExists = _mapRepository.Exists(mapId);
        bool successFullyAdded = await _mapRepository.AddOrUpdateAsync(map, cancellationToken);

        switch (mapExists)
        {
            case true when successFullyAdded:
                _logger.LogInformation("Map {MapId}/{MapCorrelationId} inserted into repository.", agvMap.MapId, agvMap.CorrelationId);
                return;

            case true when !successFullyAdded:
                _logger.LogWarning("Failed to insert Map {MapId}/{MapCorrelationId} into repository.", agvMap.MapId, agvMap.CorrelationId);
                return;

            case false when successFullyAdded:
                _logger.LogInformation("Map {MapId}/{MapCorrelationId} has been updated in repository.", agvMap.MapId, agvMap.CorrelationId);
                return;

            default:
                _logger.LogWarning("Failed to update Map {MapId}/{MapCorrelationId} in repository.", agvMap.MapId, agvMap.CorrelationId);
                break;
        }
    }
}