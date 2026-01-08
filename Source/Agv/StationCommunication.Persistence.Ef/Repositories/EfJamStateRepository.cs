using Gebhardt.Acc.Common;
using Gebhardt.Acc.StationCommunication.Domain.Models;
using Gebhardt.Acc.StationCommunication.Domain.Repositories;
using Gebhardt.Acc.StationCommunication.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.StationCommunication.Persistence.Ef.Repositories;

/// <inheritdoc cref="IJamStateRepository"/>>
[CanBeCaptiveDependency]
public class EfJamStateRepository(IDbContextFactory<StationCommunicationDbContext> contextFactory) : IJamStateRepository
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