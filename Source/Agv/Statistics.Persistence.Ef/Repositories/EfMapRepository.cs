using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Statistics.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IMapRepository"/>>.
public class EfMapRepository(StatisticsDbContext context, ILogger<EfMapRepository> logger) : EfRepository<Map, StatisticsDbContext>(context), IMapRepository
{
    /// <inheritdoc cref="IMapRepository"/>>
    public Task<Map?> GetMapAsync(Guid mapId)
    {
        return DbSet.SingleOrDefaultAsync(m => m.MapId == mapId);
    }

    /// <inheritdoc cref="IMapRepository"/>>
    public async Task ThrowIfNotExistAsync(Guid mapId)
    {
        try
        {
            await DbSet.SingleAsync(m => m.MapId == mapId);
        }
        catch (InvalidOperationException e)
        {
            logger.LogError(e, "Encountered exception while trying to fetch map {MapCorrelationId}", mapId);
            throw new MapNotFoundException(mapId);
        }
    }
}