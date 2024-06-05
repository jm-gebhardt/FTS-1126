using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Statistics.Services.EventHandler;

/// <summary>
/// Provides a handler for error-related events.
/// </summary>
[UsedImplicitly]
public class ErrorEventHandler(IStatisticsDb db): IConsumer<AgvErrorReceived>
{
    /// <summary>
    /// Handles the <see cref="AgvErrorReceived"/> event.
    /// </summary>
    public async Task Consume(ConsumeContext<AgvErrorReceived> context)
    {
        (var error, var position, string serialNumber) = context.Message;
        ICollection<Grid> grids = await db.Grids.GetAll();
       
        // Collect grid positions first to avoid duplication of
        // error counts on retry (if a position was not saved yet)
        ICollection<GridAgvPosition> gridPositions = [];
        foreach (var grid in grids)
        {
            (int xPos, int yPos) = grid.GetXyIndex(position.X, position.Y);
            var gridPosition = await db.GridAgvPositions.GetPositionAsync(grid.GridId, xPos, yPos, position.Z, serialNumber);
            gridPositions.Add(gridPosition);
        }

        foreach (var gridPosition in gridPositions)
        {
            await context.Publish(new AddError(error, gridPosition.PositionId));
        }
    }
}