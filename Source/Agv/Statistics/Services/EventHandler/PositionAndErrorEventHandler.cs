using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Memory;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvError = Gebhardt.Acc.Common.Domain.Models.AgvError;

namespace Gebhardt.Acc.Statistics.Services.EventHandler;

/// <summary>
/// Provides handlers for agv positions
/// </summary>
[UsedImplicitly]
public class PositionAndErrorEventHandler : IConsumer<AgvPositionReceived>, IConsumer<AgvErrorReceived>
{
    private const int MaxAllowedQueuedErrorsPerAgv = 20;
    private const int MaxPositionsInCachePerAgv = 1000;

    private readonly IStatisticsDb _db;
    private readonly IMessageFilter<AgvPositionReceived> _agvPositionFilter;
    private readonly ILogger<PositionAndErrorEventHandler> _logger;
    private readonly IInMemoryCache<(string agvSerialNumber, string position), (AgvPositionReceived AgvPositionReceived, List<Guid>? GridPositionIds)> _positionCache;
    private readonly IInMemoryCache<(string agvSerialNumber, string position), List<AgvError>> _errorsByAgvPositionQueue;

    /// <inheritdoc cref="PositionAndErrorEventHandler" />
    public PositionAndErrorEventHandler(
        IStatisticsDb db,
        IMessageFilter<AgvPositionReceived> agvPositionFilter,
        IInMemoryCache<(string agvSerialNumber, string position), (AgvPositionReceived, List<Guid>?)> positionCache,
        IInMemoryCache<(string agvSerialNumber, string position), List<AgvError>> errorsByAgvPositionQueue,
        ILogger<PositionAndErrorEventHandler> logger)
    {
        _db = db;
        _agvPositionFilter = agvPositionFilter;
        _logger = logger;
        _positionCache = positionCache;
        _errorsByAgvPositionQueue = errorsByAgvPositionQueue;
    }

    private static async Task HandleError<T>(AgvError error, List<Guid> gridPositionIds, ConsumeContext<T> context) where T : class
    {
        foreach (var gridPositionId in gridPositionIds)
        {
            await context.Publish(new AddError(error, gridPositionId));
        }
    }

    private void AddToPositionCache(string agvSerialNumber, AgvPositionReceived positionReceived, List<Guid>? gridPositionIds)
    {
        int count = _positionCache.Count(kvp => kvp.Key.agvSerialNumber == agvSerialNumber);
        if (count > MaxPositionsInCachePerAgv)
        {
            _positionCache.RemoveOldest(kvp => kvp.Key.agvSerialNumber == agvSerialNumber);
        }

        _positionCache.AddOrUpdate((agvSerialNumber, positionReceived.AgvPosition.ToString()), (positionReceived, gridPositionIds));
    }

    private void AddToErrorCache(string agvSerialNumber, AgvPosition position, AgvError error)
    {
        _errorsByAgvPositionQueue.TryGet((agvSerialNumber, position.ToString()), out List<AgvError>? errors);
        if (errors == null)
        {
            errors = [error];
        }
        else
        {
            errors.Add(error);
        }

        _errorsByAgvPositionQueue.AddOrUpdate((agvSerialNumber, position.ToString()), errors);

        int count = _errorsByAgvPositionQueue.Count(kvp => kvp.Key.agvSerialNumber == agvSerialNumber);
        _logger.LogDebug("AGV {AgvSerialNumber} has {Count} errors in cache", agvSerialNumber, count);
        if (count > MaxAllowedQueuedErrorsPerAgv)
        {
            _logger.LogError("More than 20 errors for AGV {AgvSerialNumber} are not handled, removing oldest", agvSerialNumber);
            _errorsByAgvPositionQueue.RemoveOldest(kvp => kvp.Key.agvSerialNumber == agvSerialNumber);
        }
    }

    private async Task<List<Guid>> AddPositionAsync<T>(AgvPositionReceived positionReceived, ConsumeContext<T> context) where T : class
    {
        var position = positionReceived.AgvPosition;
        string agvSerialNumber = positionReceived.SerialNumber;
        var messageCreatedDate = positionReceived.MessageCreatedDate;
        ICollection<Grid> grids = await _db.Grids.GetAll();
        var gridPositionIds = new List<Guid>();
        foreach (var grid in grids)
        {
            (int xPos, int yPos) = grid.GetXyIndex(position.X, position.Y);
            var gridPosition = new GridAgvPosition(grid.GridId, agvSerialNumber, position.X, position.Y, position.Z, xPos, yPos, messageCreatedDate, null);
            _db.GridAgvPositions.Add(gridPosition);
            gridPositionIds.Add(gridPosition.PositionId);
            ICollection<Map> maps = await _db.Maps.GetMapsForGridAsync(grid.GridId);

            foreach (var map in maps)
            {
                if (!map.IsPositionWithinMap(xPos, yPos, position.Z))
                {
                    _logger.LogDebug("{AgvSerialNumber}: Skip MapAgvPosition, because it's not inside {XPos}/{YPos} ({XMin}/{YMin})", agvSerialNumber, xPos, yPos, map.XMin, map.YMin);
                    continue;
                }

                var mapPosition = new MapAgvPositions(map.MapId, map, gridPosition.PositionId, gridPosition);
                _db.MapAgvPositions.Add(mapPosition);
            }
        }

        await _db.SaveAsync();

        if (_errorsByAgvPositionQueue.TryRemove((agvSerialNumber, position.ToString()), out List<AgvError>? errors))
        {
            int count = _errorsByAgvPositionQueue.Count(kvp => kvp.Key.agvSerialNumber == agvSerialNumber);
            _logger.LogDebug("Handled error for AGV {AgvSerialNumber} from cache, remaining errors in cache: {Count}", agvSerialNumber, count);
            foreach (var error in errors)
            {
                await HandleError(error, gridPositionIds, context);
            }
        }

        AddToPositionCache(agvSerialNumber, positionReceived, gridPositionIds);

        return gridPositionIds;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AgvPositionReceived> context)
    {
        var position = context.Message.AgvPosition;
        string agvSerialNumber = context.Message.SerialNumber;

        if (!await _agvPositionFilter.ShouldActAsync(context.Message))
        {
            if (!_errorsByAgvPositionQueue.TryGet((agvSerialNumber, position.ToString()), out _))
            {
                AddToPositionCache(agvSerialNumber, context.Message, null);
                return;
            }

            _logger.LogDebug("Storing additional position for AGV {AgvSerialNumber} for the error", agvSerialNumber);
        }

        await AddPositionAsync(context.Message, context);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AgvErrorReceived> context)
    {
        (var error, var position, string agvSerialNumber) = context.Message;
        if (!_positionCache.TryGet((agvSerialNumber, position.ToString()), out (AgvPositionReceived AgvPositionReceived, List<Guid>?) cacheValue))
        {
            AddToErrorCache(agvSerialNumber, position, error);
            return;
        }
        List<Guid>? gridPositionIds = cacheValue.Item2;

        if (gridPositionIds == null)
        {
            _logger.LogDebug("Storing additional position for AGV {AgvSerialNumber} as we received an error", agvSerialNumber);
            gridPositionIds = await AddPositionAsync(cacheValue.AgvPositionReceived, context);
        }

        await HandleError(error, gridPositionIds, context);
    }
}