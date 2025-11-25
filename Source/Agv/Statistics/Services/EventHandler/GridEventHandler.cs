using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.Maps;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Services.EventHandler;

/// <summary>
/// Provides handlers for grid-related events.
/// </summary>
[UsedImplicitly]
public class GridEventHandler : IConsumer<MapSaved>
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
    /// Handles the <see cref="MapSaved"/> event for the global graph.
    /// </summary>
    public async Task Consume(ConsumeContext<MapSaved> context)
    {
        var newMap = context.Message.Map;
        _logger.LogDebug("Consume MapSaved: Map is {MapId} with {NodeCount}", newMap.MapId, newMap.Nodes.Count);
        ICollection<Grid> grids = await _db.Grids.GetAll();

        foreach (var grid in grids)
        {
            var map = await _db.Maps.GetMapAsync(newMap.CorrelationId);

            ICollection<GraphNode> nodes = newMap.Nodes;
            int xMin = (int) Math.Floor(nodes.MinBy(n => n.X)!.X);
            int yMin = (int) Math.Floor(nodes.MinBy(n => n.Y)!.Y);
            int xMax = (int) Math.Ceiling(nodes.MaxBy(n => n.X)!.X);
            int yMax = (int) Math.Ceiling(nodes.MaxBy(n => n.Y)!.Y);

            if (map == null)
            {
                map = new Domain.Models.Map(newMap.CorrelationId, grid.GridId, xMin, yMin, xMax, yMax);
                _db.Maps.Add(map);
            }
            else
            {
                map.UpdateExtent(xMin, yMin, xMax, yMax);
            }

            await _db.MapGridCell.UpdateRelationsAsync(map);
        }
        await _db.SaveAsync();
    }
}