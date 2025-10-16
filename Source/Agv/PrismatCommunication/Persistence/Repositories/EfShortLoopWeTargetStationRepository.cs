using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Exceptions;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

/// <inheritdoc cref="IShortLoopWeTargetStationRepository"/>>
public class EfShortLoopWeTargetStationRepository(IDbContextFactory<PrismatCommunicationDbContext> contextFactory) : EfRepository<ShortLoopWeTargetStation, PrismatCommunicationDbContext, IDbContextFactory<PrismatCommunicationDbContext>>(contextFactory), IShortLoopWeTargetStationRepository
{
    /// <inheritdoc cref="IShortLoopWeTargetStationRepository"/>>
    public async Task<ShortLoopWeTargetStation> GetTargetStation()
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var targetStation = await context.Set<ShortLoopWeTargetStation>().SingleOrDefaultAsync();
        return targetStation ?? throw new ShortLoopWeTargetStationNotFoundException();
    }

    /// <inheritdoc cref="IShortLoopWeTargetStationRepository"/>>
    public async Task UpdateAsync(ShortLoopWeTargetStation station)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        context.Set<ShortLoopWeTargetStation>().Update(station);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task LockStation(string station)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var targetStation = await context.Set<ShortLoopWeTargetStation>().SingleOrDefaultAsync() ?? throw new ShortLoopWeTargetStationNotFoundException();
        targetStation.LockedStations.Add(station);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UnlockStation(string station)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var targetStation = await context.Set<ShortLoopWeTargetStation>().SingleOrDefaultAsync() ?? throw new ShortLoopWeTargetStationNotFoundException();
        targetStation.LockedStations.Remove(station);
        await context.SaveChangesAsync();
    }
}