using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Services.EventHandler;

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
            return;
        }

        ICollection<Grid> grids = await _db.Grids.GetAll();
        foreach (var grid in grids)
        {
            (int xPos, int yPos) = grid.GetXyIndex(position.X, position.Y);
            var gridPosition = new GridAgvPosition(grid.GridId, agv, position.X, position.Y, position.Z, xPos, yPos, context.Message.MessageCreatedDate, null);
            _db.GridAgvPositions.Add(gridPosition);

            ICollection<Map> maps = await _db.Maps.GetMapsForGridAsync(grid.GridId);

            foreach (var map in maps)
            {
                if (!map.IsPositionWithinMap(xPos, yPos, position.Z))
                {
                    _logger.LogDebug("{AgvSerialNumber}: Skip MapAgvPosition, because it's not inside {XPos}/{YPos} ({XMin}/{YMin})", context.Message.SerialNumber, xPos, yPos, map.XMin, map.YMin);
                    continue;
                }

                var mapPosition = new MapAgvPositions(map.MapId, map, gridPosition.PositionId, gridPosition);
                _db.MapAgvPositions.Add(mapPosition);
            }
        }

        await _db.SaveAsync();
    }
}