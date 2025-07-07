using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Maps;
using Gebhardt.Acc.Statistics.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Extensions;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Map = Gebhardt.Acc.Common.Domain.Models.Graphs.Map;

namespace Gebhardt.Acc.Statistics.Services.EventHandler;

/// <summary>
/// Provides handlers for grid-related events.
/// </summary>
[UsedImplicitly]
public class GridEventHandler :
    IConsumer<GraphUpdated>,
    IConsumer<MapSaved>
{
    private readonly IStatisticsDb _db;
    private readonly ILogger<GridEventHandler> _logger;

    /// <inheritdoc cref="GridEventHandler" />
    public GridEventHandler(IStatisticsDb db, ILogger<GridEventHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GraphUpdated> context)
    {
        List<Map>? maps = context.Message.Maps;
        if (maps == null)
        {
            _logger.LogTrace("{Module}: context.Message.Maps is empty", nameof(GraphUpdated));
        }

        var agvGraph = context.Message.Graph;
        ICollection<Guid> gridIds = await _db.Grids.GetAllIdsAsync();

        foreach (var gridId in gridIds)
        {
            var grid = await _db.Grids.GetByIdAsync(gridId);
            var agvGrid = agvGraph.Nodes.ToGrid(xStep: grid.XStep, yStep: grid.YStep, grid.GridName);
            try
            {
                grid.TryExpandByGrid(agvGrid);
            }
            catch (IncompatibleGridException)
            {
                _logger.LogError("Incompatible grids: {GridXStep}/{AgvGridXStep}, {GridYStep}/{AgvGridYStep}", grid.XStep, agvGrid.XStep, grid.YStep, agvGrid.YStep);
            }
        }
        await _db.SaveAsync();
    }

    /// <summary>
    /// Handles the <see cref="MapSaved"/> event for the global graph.
    /// </summary>
    public async Task Consume(ConsumeContext<MapSaved> context)
    {
        var agvMap = context.Message.Map;
        _logger.LogDebug("Consume MapSaved: agvMap is {MapId} with {NodeCount}", agvMap.MapId, agvMap.Nodes.Count);
        ICollection<Guid> gridIds = await _db.Grids.GetAllIdsAsync();

        foreach (var gridId in gridIds)
        {
            var grid = await _db.Grids.GetByIdAsync(gridId);
            ICollection<Domain.Models.Map> mapsForGrid = await _db.Maps.GetMapsForGridAsync(grid.GridId);
            var map = mapsForGrid.FirstOrDefault(m => m.MapId == agvMap.CorrelationId);

            var gridFromMap = agvMap.Nodes.ToGrid(xStep: grid.XStep, yStep: grid.YStep, grid.GridName);
            (int xiMin, int yiMin) = grid.GetXyIndex(gridFromMap.XStart, gridFromMap.YStart);
            (double gridXMax, double gridYMax) = gridFromMap.GetXyMax();
            (int xiMax, int yiMax) = grid.GetXyIndex(gridXMax, gridYMax);

            if (map == null)
            {
                _db.Maps.Add(new Domain.Models.Map(agvMap.CorrelationId, grid.GridId, xiMin, yiMin, xiMax, yiMax, agvMap.Nodes.Select(n => n.Z).First()));
                continue;
            }
            map.UpdateExtent(xiMin, yiMin, xiMax, yiMax);
        }
        await _db.SaveAsync();
    }
}