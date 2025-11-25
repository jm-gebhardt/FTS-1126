using Gebhardt.Acc.Common.Memory;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Services.EventHandler;

/// <summary>
/// Handles the reception of AgvPositionReceived events.
/// </summary>
[UsedImplicitly]
public class AgvPositionReceivedHandler : GridCellAttachedHandlerBase<AgvPositionReceived>
{
    private readonly IMessageFilter<AgvPositionReceived> _filter;
    private readonly ICache<Guid, List<(GridCell GridCell, DateTime EntryTime)>> _gridCellIdEntriesByAgv;

    /// <inheritdoc />
    public AgvPositionReceivedHandler(
        IMessageFilter<AgvPositionReceived> filter,
        IStatisticsDb statisticsDb,
        ICache<Guid, List<(GridCell, DateTime)>> gridCellIdIdEntriesByAgv,
        IPublishEndpoint publishEndpoint,
        ILogger<AgvPositionReceivedHandler> logger)
        : base(statisticsDb, publishEndpoint, logger)
    {
        _filter = filter;
        _gridCellIdEntriesByAgv = gridCellIdIdEntriesByAgv;
    }

    /// <inheritdoc />
    protected override async Task HandleAsync(AgvPositionReceived message, List<GridCell> gridCells)
    {
        if (!await _filter.ShouldActAsync(message))
        {
            return;
        }

        var agvCorrelationId = message.CorrelationId;
        if (!_gridCellIdEntriesByAgv.TryGet(agvCorrelationId, out List<(GridCell, DateTime)>? previousGridEntries))
        {
            _gridCellIdEntriesByAgv.AddOrUpdate(agvCorrelationId, gridCells.Select(id => (id, message.MessageCreatedDate)).ToList());
            return;
        }

        List<(GridCell GridCell, DateTime EntryTime)> leftGridCells = previousGridEntries.Where(e => !gridCells.Contains(e.Item1)).ToList();

        foreach (var leftGridCell in leftGridCells)
        {
            var duration = message.MessageCreatedDate - leftGridCell.EntryTime;
            if (duration > TimeSpan.Zero)
            {
                var gridCellStay = new GridCellStay(agvCorrelationId,
                    leftGridCell.GridCell,
                    leftGridCell.EntryTime,
                    duration)
                {
                    // for serialization purposes
                    GridCell = null!
                };
                await PublishEndpoint.Publish(new AddGridCellStay(
                    gridCellStay));
            }

            Logger.LogDebug("AGV {SerialNumber} stayed in grid cell {GridCellId} for {Duration}", message.SerialNumber, leftGridCell.GridCell.CellId, duration);
        }

        List<(GridCell, DateTime EntryTime)> stillPresent = previousGridEntries.Where(e => gridCells.Contains(e.Item1)).ToList();
        List<(GridCell id, DateTime EntryTime)> newEntries = gridCells.Except(previousGridEntries.Select(e => e.Item1))
            .Select(id => (id, message.MessageCreatedDate))
            .ToList();

        _gridCellIdEntriesByAgv.AddOrUpdate(agvCorrelationId, stillPresent.Concat(newEntries).ToList());
    }
}