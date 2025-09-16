using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.DataAccess;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IStationRepository"/>>
public class EfStationRepository(IDbContextFactory<KaeserSidecarDbContext> contextFactory, KaeserGraphConfiguration configuration) :
    EfRepository<StationOccupancy, KaeserSidecarDbContext, IDbContextFactory<KaeserSidecarDbContext>>(contextFactory), IStationRepository
{
    /// <inheritdoc cref="IStationRepository"/>>
    public async Task OccupyStation(Guid stationId, Guid agv)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var stationOccupancy = await context.Set<StationOccupancy>().Where(s => s.StationId == stationId).SingleOrDefaultAsync();
        if (stationOccupancy != null)
        {
            stationOccupancy.Add(agv);
            await context.SaveChangesAsync();
            return;
        }

        stationOccupancy = new StationOccupancy(stationId, "", 1, agv);
        await AddAsync(stationOccupancy);
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task FreeStation(Guid stationId)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var stationOccupancy = await context.Set<StationOccupancy>().Where(s => s.StationId == stationId).SingleOrDefaultAsync();
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
        List<string> stations = occupancies.Select(o => o.StationName).ToList();
        IEnumerable<string> missingStations = configuration.ConveyorStationMapping.Keys.Except(stations);
        foreach (string station in missingStations)
        {
            occupancies.Add(new StationOccupancy(station.ToGuid(), station, 0, null));
        }
        return occupancies;
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task<StationOccupancy> GetOccupancyOfStation(Guid stationId)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var stationOccupancy = await context.Set<StationOccupancy>().Where(s => s.StationId == stationId).SingleOrDefaultAsync();
        return stationOccupancy ?? new StationOccupancy(stationId, "", 0, null);
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task<StationOccupancy?> GetOccupancyByAgvCorrelationId(Guid agvCorrelationId)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<StationOccupancy>().FirstOrDefaultAsync(s => s.AgvCorrelationId == agvCorrelationId);
    }
}