using System.Collections.Concurrent;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="IStationRepository"/>>
public class StationRepository : IStationRepository
{
    private readonly ConcurrentDictionary<string, List<Guid>> _stationOccupancies = new();
    private readonly ILogger<StationRepository> _logger;

    /// <inheritdoc cref="IStationRepository"/>>
    public StationRepository(ILogger<StationRepository> logger, KaeserGraphConfiguration configuration)
    {
        _logger = logger;
        foreach (KeyValuePair<string, string> station in configuration.ConveyorStationMapping)
        {
            _stationOccupancies.TryAdd(station.Key, []);
        }
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public void OccupyStation(string station, Guid agv)
    {
        _stationOccupancies.AddOrUpdate(station,
            addValueFactory: (_) => [agv],
            updateValueFactory: (_, oldValue) =>
            {
                oldValue.Add(agv);
                return oldValue;
            });
        _logger.LogInformation("Added agv {Agv} to station {Station}, new occupancy is {Occupancy}", agv, station, _stationOccupancies[station].Count);
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public void FreeStation(string station, Guid agv)
    {
        if (_stationOccupancies.TryGetValue(station, out List<Guid>? agvs))
        {
            agvs.Remove(agv);
        }
        _logger.LogInformation("Removed agv {Agv} from station {Station}, new occupancy is {Occupancy}", agv, station, _stationOccupancies[station].Count);
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public ICollection<StationOccupancy> GetStationOccupancy()
    {
        return _stationOccupancies.Select(kvp => new StationOccupancy(kvp.Key, kvp.Value.Count)).OrderBy(s => s.Station).ToList();
    }
}