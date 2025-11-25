using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="EfMapGridCellRepository"/>>.
public class EfMapGridCellRepository(StatisticsDbContext context) : EfRepository<MapGridCell, StatisticsDbContext>(context), IMapGridCellRepository
{
    /// <inheritdoc cref="EfMapGridCellRepository"/>>.
    public async Task<ICollection<MapGridCell>> GetMapAgvPositionsForMapAsync(Guid mapId)
    {
        return await DbSet.Where(mp => mp.MapId == mapId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task UpdateRelationsAsync(Map map)
    {
        List<GridCell> gridCells = await Context.Set<GridCell>()
            .Where(gc => gc.XMin >= map.XMin && gc.XMax <= map.XMax
                                             && gc.YMin >= map.YMin && gc.YMax <= map.YMax)
            .ToListAsync();

        List<MapGridCell> all = gridCells.Select(gc => new MapGridCell(map, gc)).ToList();

        List<Guid> existingKeys = await DbSet
            .Where(m => all.Select(c => c.GridCellId).Contains(m.GridCellId) && m.MapId == map.MapId)
            .Select(m => m.GridCellId)
            .ToListAsync();

        List<MapGridCell> toInsert = all
            .Where(c => !existingKeys.Contains(c.GridCellId))
            .ToList();

        AddRange(toInsert);
    }
}