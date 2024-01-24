using Microsoft.EntityFrameworkCore;
using Statistics.Domain.Models;
using Statistics.Domain.ReadModels;
using Statistics.Domain.Repositories;
using Statistics.Persistence.Ef.DataAccess;

namespace Statistics.Persistence.Ef.Repositories;

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