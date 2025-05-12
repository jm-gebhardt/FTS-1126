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
    public Task OccupyStation(string station, Guid agv)
    {
        _stationOccupancies.AddOrUpdate(station,
            addValueFactory: (_) => [agv],
            updateValueFactory: (_, oldValue) =>
            {
                if (oldValue.Count > 0)
                {
                    oldValue[0] = agv;
                }
                else
                {
                    oldValue.Add(agv);
                }
                return oldValue;
            });
        _logger.LogInformation("Added agv {Agv} to station {Station}, new occupancy is {Occupancy}", agv, station, _stationOccupancies[station].Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public Task FreeStation(string station, Guid agv)
    {
        if (_stationOccupancies.TryGetValue(station, out List<Guid>? agvs))
        {
            agvs.Remove(agv);
        }
        _logger.LogInformation("Removed agv {Agv} from station {Station}, new occupancy is {Occupancy}", agv, station, _stationOccupancies[station].Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public Task<ICollection<StationOccupancy>> GetStationOccupancy()
    {
        return Task.FromResult<ICollection<StationOccupancy>>(
            _stationOccupancies.Select(kvp => new StationOccupancy(kvp.Key, kvp.Value.Count, kvp.Value.Cast<Guid?>().SingleOrDefault())).OrderBy(s => s.Station).ToList());
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public Task<StationOccupancy?> GetOccupancyByAgvCorrelationId(Guid agvCorrelationId)
    {
        return Task.FromResult(_stationOccupancies.Select(kvp => new StationOccupancy(kvp.Key,
                kvp.Value.Count,
                kvp.Value.SingleOrDefault()))
            .SingleOrDefault(s => s.AgvCorrelationId == agvCorrelationId));
    }
    
    /// <summary>
    /// Sets the occupancy of the stations.
    /// </summary>
    /// <param name="occupancies"></param>
    public void WithOccupancies(ICollection<StationOccupancy> occupancies)
    {
        foreach (var occupancy in occupancies)
        {
            for (int i = 0; i < occupancy.Count; i++)
            {
                OccupyStation(occupancy.Station, (Guid) occupancy.AgvCorrelationId!);
            }
        }
    }
}