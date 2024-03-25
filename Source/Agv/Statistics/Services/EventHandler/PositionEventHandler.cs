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
            _logger.LogDebug($"{context.Message.SerialNumber}: skip AgvPosition.");
            return;
        }
        _logger.LogDebug($"{context.Message.SerialNumber}: use AgvPosition.");

        ICollection<Grid> grids = await _db.Grids.GetAll();
        foreach (var grid in grids)
        {
            (int xPos, int yPos) = grid.GetXyIndex(position.X, position.Y);
            var gridPosition = new AgvGridPosition(grid.GridId, agv, position.X, position.Y, xPos, yPos, DateTime.UtcNow);
            _db.GridPositions.Add(gridPosition);
        }

        await _db.SaveAsync();
    }
}