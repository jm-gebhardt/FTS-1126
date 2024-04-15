using Gebhardt.StoreWare.Agv.Common.Messages.Graphs.Events;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Repositories;
using Gebhardt.StoreWare.Agv.Statistics.Extensions;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Map = Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs.Map;

namespace Gebhardt.StoreWare.Agv.Statistics.Services.EventHandler;

/// <summary>
/// Provides handlers for grid-related events.
/// </summary>
[UsedImplicitly]
public class GridEventHandler :
    IConsumer<GraphChanged>
{
    private readonly IStatisticsDb _db;
    private readonly ILogger<GridEventHandler> _logger;

    /// <inheritdoc cref="GridEventHandler" />
    public GridEventHandler(IStatisticsDb db, ILogger<GridEventHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="GraphChanged"/> event for the global graph.
    /// </summary>
    public async Task Consume(ConsumeContext<GraphChanged> context)
    {
        List<Map>? maps = context.Message.Maps;
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
            if (maps == null)
            {
                continue;
            }

            ICollection<Domain.Models.Map> mapsForGrid = await _db.Maps.GetMapsForGridAsync(grid.GridId);

            //Create map objects for the grid based on the x and y index of the maximum and
            //minimum x and y positions covered by the map.
            foreach (var agvMap in maps)
            {
                var map = mapsForGrid.FirstOrDefault(m => m.MapId == agvMap.CorrelationId);

                var gridFromMap = agvMap.Nodes.ToGrid(xStep: grid.XStep, yStep: grid.YStep, grid.GridName);
                (int xiMin, int yiMin) = grid.GetXyIndex(gridFromMap.XStart, gridFromMap.YStart);
                (double gridXMax, double gridYMax) = gridFromMap.GetXyMax();
                (int xiMax, int yiMax) = grid.GetXyIndex(gridXMax, gridYMax);

                if (map == null)
                {
                    _db.Maps.Add(new Domain.Models.Map(agvMap.CorrelationId, grid.GridId, xiMin, yiMin, xiMax, yiMax));
                    continue;
                }
                map.UpdateExtent(xiMin, yiMin, xiMax, yiMax);
            }
        }
        await _db.SaveAsync();
    }
}