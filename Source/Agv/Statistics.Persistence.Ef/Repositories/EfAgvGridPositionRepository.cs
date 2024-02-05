using Gebhardt.StoreWare.Agv.Statistics.Domain.Models;
using Gebhardt.StoreWare.Agv.Statistics.Domain.ReadModels;
using Gebhardt.StoreWare.Agv.Statistics.Domain.Repositories;
using Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IAgvGridPositionRepository"/>>.
public class EfAgvGridPositionRepository(StatisticsDbContext context) : EfRepository<AgvGridPosition, StatisticsDbContext>(context), IAgvGridPositionRepository
{
    /// <inheritdoc cref="IAgvGridPositionRepository"/>>
    public async Task<PositionFrequency> GetPositionFrequencyForGrid(Guid gridId)
    {
        ICollection<ICollection<float>> frequencies = await DbSet
            .Where(p => p.GridId == gridId)
            .GroupBy(entry => new { entry.XIndex, entry.YIndex })
            .Select(group => new List<float>
            {
                group.Key.XIndex, group.Key.YIndex, group.Count()
            } as ICollection<float>).ToListAsync();
        return new PositionFrequency(gridId, frequencies);
    }
}