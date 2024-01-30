using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using JetBrains.Annotations;
using MassTransit;
using Statistics.Domain.Models;
using Statistics.Domain.Repositories;

namespace Gebhardt.StoreWare.Agv.Statistics.Services.EventHandler;

/// <summary>
/// Provides handlers for agv positions
/// </summary>
[UsedImplicitly]
public class PositionEventHandler : IConsumer<AgvPositionReceived>
{
    private readonly IStatisticsDb _db;

    /// <inheritdoc cref="PositionEventHandler" />
    public PositionEventHandler(IStatisticsDb db)
    {
        _db = db;
    }

    /// <summary>
    /// Handles the <see cref="AgvPositionReceived"/> event
    /// </summary>
    public async Task Consume(ConsumeContext<AgvPositionReceived> context)
    {
        var position = context.Message.AgvPosition;
        string agv = context.Message.AgvId;

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