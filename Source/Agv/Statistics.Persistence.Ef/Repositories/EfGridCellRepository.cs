using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Extensions;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IGridCellRepository"/>
public class EfGridCellRepository(StatisticsDbContext context) : EfRepository<GridCell, StatisticsDbContext>(context), IGridCellRepository
{
    /// <inheritdoc />
    public async Task<List<GridCell>> GetOrAddGridCellsForCoordinatesAsync(decimal x, decimal y)
    {
        Dictionary<Grid, GridCell?> allGrids = await Context.Set<Grid>()
            .Include(g => g.Maps)
            .Select(g => new
            {
                Grid = g,
                Cell = g.GridCells
                    .FirstOrDefault(c => x >= c.XMin && x <= c.XMax &&
                                         y >= c.YMin && y <= c.YMax)
            })
            .ToDictionaryAsync(k => k.Grid, v => v.Cell);

        foreach (var (grid, _) in allGrids.Where(g => g.Value == null))
        {
            var newCell = grid.GetGridCellForPosition(x, y);
            Add(newCell);
            allGrids[grid] = newCell;

            foreach (var map in grid.Maps!)
            {
                if (!map.IsWithinMap(newCell.XIndex, newCell.YIndex))
                {
                    continue;
                }
                var mapGridCell = new MapGridCell(map, newCell);
                Context.Add(mapGridCell);
            }
        }

        await Context.SaveChangesAsync();

        return allGrids.Select(c => c.Value!).ToList();
    }
}