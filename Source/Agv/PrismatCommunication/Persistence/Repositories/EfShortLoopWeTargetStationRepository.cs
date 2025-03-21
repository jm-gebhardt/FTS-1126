using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Exceptions;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

/// <inheritdoc cref="IShortLoopWeTargetStationRepository"/>>
public class EfShortLoopWeTargetStationRepository(PrismatCommunicationDbContext context) : EfRepository<ShortLoopWeTargetStation, PrismatCommunicationDbContext>(context), IShortLoopWeTargetStationRepository
{
    /// <inheritdoc cref="IShortLoopWeTargetStationRepository"/>>
    public async Task<ShortLoopWeTargetStation> GetTargetStation()
    {
        var targetStation = await Context.Set<ShortLoopWeTargetStation>().SingleOrDefaultAsync();
        return targetStation ?? throw new ShortLoopWeTargetStationNotFoundException();
    }

    /// <inheritdoc cref="IShortLoopWeTargetStationRepository"/>>
    public async Task UpdateAsync(ShortLoopWeTargetStation station)
    {
        Context.Set<ShortLoopWeTargetStation>().Update(station);
        await Context.SaveChangesAsync();
    }
}