using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IStationRepository"/>>
public class EfStationRepository(IDbContextFactory<KaeserSidecarDbContext> contextFactory, KaeserGraphConfiguration configuration) : EfRepository<StationOccupancy, KaeserSidecarDbContext, IDbContextFactory<KaeserSidecarDbContext>>(contextFactory), IStationRepository
{
    /// <inheritdoc cref="IStationRepository"/>>
    public async Task OccupyStation(string station, Guid agv)
    {
        await  using var context = await ContextFactory.CreateDbContextAsync();
        var stationOccupancy = await context.Set<StationOccupancy>().Where(s => s.Station == station).SingleOrDefaultAsync();
        if (stationOccupancy != null)
        {
            stationOccupancy.Add(agv);
            await context.SaveChangesAsync();
            return;
        }
        stationOccupancy = new StationOccupancy(station, 1, agv);
        await AddAsync(stationOccupancy);
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task FreeStation(string station, Guid agv)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var stationOccupancy = await context.Set<StationOccupancy>().Where(s => s.Station == station).SingleOrDefaultAsync();
        if (stationOccupancy != null)
        {
            stationOccupancy.Remove();
            await context.SaveChangesAsync();
        }
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task<ICollection<StationOccupancy>> GetStationOccupancy()
    {
        ICollection<StationOccupancy> occupancies = await GetAll();
        List<string> stations = occupancies.Select(o => o.Station).ToList();
        IEnumerable<string> missingStations = configuration.ConveyorStationMapping.Keys.Except(stations);
        foreach (string station in missingStations)
        {
           occupancies.Add(new StationOccupancy(station, 0, null));
        }
        return occupancies;
    }
}