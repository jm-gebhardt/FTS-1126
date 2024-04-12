using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Services.Filters;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.Statistics.Services.EventHandler;

/// <summary>
/// Provides handlers for agv positions
/// </summary>
[UsedImplicitly]
public class PositionEventHandler : IConsumer<AgvPositionReceived>
{
    private readonly IStatisticsDb _db;
    private readonly IMessageFilter<AgvPositionReceived> _agvPositionFilter;
    private readonly ILogger<PositionEventHandler> _logger;

    /// <inheritdoc cref="PositionEventHandler" />
    public PositionEventHandler(
        IStatisticsDb db,
        IMessageFilter<AgvPositionReceived> agvPositionFilter,
        ILogger<PositionEventHandler> logger
    )
    {
        _db = db;
        _agvPositionFilter = agvPositionFilter;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="AgvPositionReceived"/> event
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPositionReceived> context)
    {
        var position = context.Message.AgvPosition;
        string agv = context.Message.SerialNumber;

        if (!await _agvPositionFilter.ShouldActAsync(context.Message))
        {
            _logger.LogDebug("{AgvSerialNumber}: Skip AgvPosition", context.Message.SerialNumber);
            return;
        }
        _logger.LogDebug("{AgvSerialNumber}: Use AgvPosition", context.Message.SerialNumber);

        ICollection<Grid> grids = await _db.Grids.GetAll();
        foreach (var grid in grids)
        {
            (int xPos, int yPos) = grid.GetXyIndex(position.X, position.Y);
            var gridPosition = new GridAgvPosition(grid.GridId, agv, position.X, position.Y, xPos, yPos, context.Message.MessageCreatedDate, null);
            _db.GridAgvPositions.Add(gridPosition);

            ICollection<Map> maps = await _db.Maps.GetMapsForGridAsync(grid.GridId);

            foreach (var map in maps)
            {
                if (!map.IsPositionWithinMap(xPos, yPos))
                {
                    _logger.LogDebug("{AgvSerialNumber}: Skip MapAgvPosition, because it's not inside", context.Message.SerialNumber);
                    continue;
                }

                var mapPosition = new MapAgvPositions(map.MapId, map, gridPosition.PositionId, gridPosition);
                _db.MapAgvPositions.Add(mapPosition);
            }
        }

        await _db.SaveAsync();
    }
}