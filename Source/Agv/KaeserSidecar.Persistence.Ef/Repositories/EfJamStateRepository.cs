using Gebhardt.Acc.Common;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IJamStateRepository"/>>
[CanBeCaptiveDependency]
public class EfJamStateRepository(IDbContextFactory<KaeserSidecarDbContext> contextFactory) : IJamStateRepository
{
    /// <inheritdoc cref="IJamStateRepository"/>>
    public async Task AddAsync(JamState jamState)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        await context.Set<JamState>().AddAsync(jamState);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc cref="IJamStateRepository"/>>
    public async Task UpdateAsync(JamState jamState)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        context.Set<JamState>().Update(jamState);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc cref="IJamStateRepository"/>>
    public async Task<JamState?> GetByStationAsync(Guid stationId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Set<JamState>().SingleOrDefaultAsync(j => j.StationId == stationId);
    }
}