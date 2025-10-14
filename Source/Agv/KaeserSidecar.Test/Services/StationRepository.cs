using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.KaeserSidecar.Domain.Models;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services;

/// <inheritdoc cref="IStationRepository"/>>
public class StationRepository : IStationRepository
{
    private readonly Dictionary<Guid, StationOccupancy> _stationOccupancies = new();

    /// <inheritdoc cref="IStationRepository"/>>
    public StationRepository(KaeserGraphConfiguration configuration)
    {
        foreach (KeyValuePair<string, string> station in configuration.ConveyorStationMapping)
        {
            var stationId = station.Key.ToGuid();
            _stationOccupancies.Add(stationId, new StationOccupancy(stationId, 0, null));
        }
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public Task OccupyStation(Guid stationId, Guid agv)
    {
        if (_stationOccupancies.TryGetValue(stationId, out var occupancy))
        {
            occupancy.Add(agv);
        }
        else
        {
            var stationOccupancy = new StationOccupancy(stationId, 1, agv);
            _stationOccupancies.Add(stationId, stationOccupancy);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public Task FreeStation(Guid stationId)
    {
        if (_stationOccupancies.TryGetValue(stationId, out var occupancy))
        {
            occupancy.Remove();
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public Task<ICollection<StationOccupancy>> GetStationOccupancy()
    {
        return Task.FromResult<ICollection<StationOccupancy>>(_stationOccupancies.Values);
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public Task<StationOccupancy> GetOccupancyOfStation(Guid stationId)
    {
        if (!_stationOccupancies.TryGetValue(stationId, out var occupancy))
        {
            occupancy = new StationOccupancy(stationId, 0, null);
            _stationOccupancies.Add(stationId, occupancy);
        }
        return Task.FromResult(occupancy);
    }

    /// <inheritdoc cref="IStationRepository"/>>
    public Task<StationOccupancy?> GetOccupancyByAgvCorrelationId(Guid agvCorrelationId)
    {
        var occupancy = _stationOccupancies.Values.FirstOrDefault(s => s.AgvCorrelationId == agvCorrelationId);
        return Task.FromResult(occupancy);
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
                OccupyStation(occupancy.StationId, (Guid) occupancy.AgvCorrelationId!);
            }
        }
    }
}