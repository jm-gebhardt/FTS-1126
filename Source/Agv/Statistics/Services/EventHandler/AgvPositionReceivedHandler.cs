using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Statistics.Cache;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Messages.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Services.EventHandler;

/// <summary>
/// Handles the reception and persistence of AGV position-related messages,
/// subscribing to the <see cref="AgvPositionReceived"/> event and the <see cref="ForcePersistAgvPosition"/> command.
/// </summary>
[UsedImplicitly]
public class AgvPositionReceivedHandler : IConsumer<AgvPositionReceived>, IConsumer<ForcePersistAgvPosition>
{
    private readonly IMessageFilter<AgvPositionReceived> _filter;
    private readonly IStatisticsDb _db;
    private readonly IAgvScopedPositionCache<Guid> _positionCache;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<AgvPositionReceivedHandler> _logger;

    /// <inheritdoc cref="AgvPositionReceivedHandler" />
    public AgvPositionReceivedHandler(
        IMessageFilter<AgvPositionReceived> filter,
        IStatisticsDb db,
        IAgvScopedPositionCache<Guid> positionCache,
        IPublishEndpoint publishEndpoint,
        ILogger<AgvPositionReceivedHandler> logger)
    {
        _filter = filter;
        _db = db;
        _positionCache = positionCache;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    private async Task PersistPositionAsync(string serial, AgvPosition pos, DateTime timestamp)
    {
        try
        {
            ICollection<Grid> grids = await _db.Grids.GetAll();
            var gridPositionIds = new List<Guid>();

            foreach (var grid in grids)
            {
                var (x, y) = grid.GetXyIndex(pos.X, pos.Y);
                var gridPos = new GridAgvPosition(grid.GridId, serial, pos.X, pos.Y, pos.Z, x, y, timestamp, null);
                _db.GridAgvPositions.Add(gridPos);
                gridPositionIds.Add(gridPos.PositionId);

                ICollection<Map> maps = await _db.Maps.GetMapsForGridAsync(grid.GridId);
                foreach (var map in maps)
                {
                    if (map.IsPositionWithinMap(x, y, pos.Z))
                    {
                        var mapPos = new MapAgvPositions(map.MapId, map, gridPos.PositionId, gridPos);
                        _db.MapAgvPositions.Add(mapPos);
                    }
                }
            }

            await _db.SaveAsync();

            _positionCache.AddItem(serial, pos, timestamp, gridPositionIds);

            await _publishEndpoint.Publish(new GridPositionPersisted(serial, pos, timestamp, gridPositionIds));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to persist AGV position for serial {SerialNumber} at position {Position}", serial, pos);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AgvPositionReceived> context)
    {
        var msg = context.Message;

        if (!await _filter.ShouldActAsync(msg))
        {
            return;
        }

        await PersistPositionAsync(msg.SerialNumber, msg.AgvPosition, msg.MessageCreatedDate);
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<ForcePersistAgvPosition> context)
    {
        _logger.LogDebug("Trying to force persisting position for AGV {SerialNumber} at {Position} ({Timestamp})",
            context.Message.SerialNumber, context.Message.AgvPosition, context.Message.MessageCreatedDate);

        if (_positionCache.TryGet(context.Message.SerialNumber, context.Message.AgvPosition, context.Message.MessageCreatedDate, out List<Guid>? cacheValue) && cacheValue.Count > 0)
        {
            _logger.LogDebug("Position {Position} for AGV {SerialNumber} already exists in DB, no need to force persistence",
                context.Message.AgvPosition, context.Message.SerialNumber);
            return Task.CompletedTask;
        }

        var msg = context.Message;
        return PersistPositionAsync(msg.SerialNumber, msg.AgvPosition, msg.MessageCreatedDate);
    }
}