using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Persistence.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.PrismatCommunication.Persistence.Repositories;

/// <inheritdoc cref="IStationRepository"/>>
public class EfStationRepository(PrismatCommunicationDbContext context): EfRepository<Station, PrismatCommunicationDbContext>(context), IStationRepository
{
    private readonly PrismatCommunicationDbContext _context = context;

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task AddOrUpdateStationsOfGroup(ICollection<Station> stations, Guid groupId)
    {
        List<Station> existingStations = await _context.Set<Station>().Where(s => s.GroupId == groupId).ToListAsync();
        List<string> removedStationSources = existingStations.Select(s => s.Source).Except(stations.Select(s => s.Source)).ToList();
        List<string> newStationSources = stations.Select(s => s.Source).Except(existingStations.Select(s => s.Source)).ToList();
        List<Station> updatedStations = stations.Where(s => s.Equals(existingStations.SingleOrDefault(e => e.Source == s.Source))).ToList();
        bool stationsUpdated = removedStationSources.Count > 0 || newStationSources.Count > 0 || updatedStations.Count > 0;
        if (!stationsUpdated)
        {
            return;
        }

        if (newStationSources.Count > 0)
        {
            IEnumerable<Station> newStations = stations.Where(s => newStationSources.Contains(s.Source));
            await _context.AddRangeAsync(newStations);
        }

        if (removedStationSources.Count > 0)
        {
            IEnumerable<Station> removedStations = existingStations.Where(s => removedStationSources.Contains(s.Source));
            _context.RemoveRange(removedStations);
        }

        if (updatedStations.Count > 0)
        {
            _context.UpdateRange(updatedStations);
        }

        await _context.SaveChangesAsync();
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task<Station?> GetStationByOccupyingAgv(Guid agvCorrelationId)
    {
        return await _context.Set<Station>().SingleOrDefaultAsync(s => s.AgvCorrelationId == agvCorrelationId);
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task<Station?> GetStationByNode(string nodeId)
    {
        return await _context.Set<Station>().SingleOrDefaultAsync(s => s.Node == nodeId);
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public async Task<StationGroup?> GetStationGroup(string source, string target)
    {
        var stationGroup = await _context.Set<Station>()
            .Where(s => s.Source == source || s.Target == target)
            .Select(s => new
            {
                ReferenceStation = s,
                Stations = _context.Set<Station>()
                    .Where(gs => gs.GroupId == s.GroupId)
                    .ToList()
            })
            .SingleOrDefaultAsync();

        return stationGroup == null ? null : new StationGroup(stationGroup.Stations);
    }
}