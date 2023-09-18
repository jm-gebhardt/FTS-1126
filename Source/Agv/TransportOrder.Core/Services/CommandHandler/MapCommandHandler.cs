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
public class MapCommandHandler: IConsumer<SaveMap>
{
    private readonly IMapRegistry _mapRegistry;
    private readonly ILogger<MapCommandHandler> _logger;

    /// <summary>
    /// Initializes a new <see cref="MapCommandHandler"/>.
    /// </summary>

    public MapCommandHandler(IMapRegistry mapRegistry, ILogger<MapCommandHandler> logger)
    {
        _mapRegistry = mapRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="SaveMap"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<SaveMap> context)
    {
        var (agvMap, mapId)  = context.Message;
        var cancellationToken = context.CancellationToken;

        var map = new Map(
            mapId,
            agvMap.MapId,
            Convert.FromBase64String(agvMap.ImageData),
            agvMap.Resolution,
            agvMap.XOffset,
            agvMap.YOffset,
            agvMap.ModificationDate
        );

        bool mapExists = _mapRegistry.Exists(mapId);
        bool successFullyAdded = await _mapRegistry.AddOrUpdateAsync(map, cancellationToken);

        switch (mapExists)
        {
            case true when successFullyAdded:
                _logger.LogInformation("Map {MapId}/{MapCorrelationId} inserted into Registry", agvMap.MapId, agvMap.CorrelationId);
                return;
            case true when !successFullyAdded:
                _logger.LogWarning("Failed to insert Map {MapId}/{MapCorrelationId} into Registry", agvMap.MapId, agvMap.CorrelationId);
                return;
            case false when successFullyAdded:
                _logger.LogInformation("Map {MapId}/{MapCorrelationId} has been updated in Registry", agvMap.MapId, agvMap.CorrelationId);
                return;
            default:
                _logger.LogWarning("Failed to update Map {MapId}/{MapCorrelationId} in Registry", agvMap.MapId, agvMap.CorrelationId);
                break;
        }
    }
}