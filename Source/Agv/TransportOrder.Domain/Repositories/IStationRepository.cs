using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Station;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for station-related requests.
/// </summary>
public interface IStationRepository
{
    /// <summary>
    /// Add all stations for an agv graph.
    /// </summary>
    /// <param name="agvGraph">The agv graph.</param>
    /// <returns></returns>
    Task AddStations(AgvGraph agvGraph);

    /// <summary>
    /// Get a single station by Id.
    /// </summary>
    /// <param name="id">The id of the station.</param>
    /// <returns>The station with the given id.</returns>
    Task<Station> GetStation(Guid id);

    /// <summary>
    /// Gets the base information of the station (w/o agvs and node state)
    /// </summary>
    /// <param name="id">The name of the station.</param>
    /// <returns>The station base information with the given name or null.</returns>
    Task<Station?> GetRequiredStationBaseInformation(Guid id);

    /// <summary>
    /// Get all stations.
    /// </summary>
    /// <param name="mapId">Optional map name.</param>
    /// <returns>All stations (optionally on the given map).</returns>
    Task<ICollection<Station>> GetStations(Guid? mapId = null);

    /// <summary>
    /// Get a stations ordered in the order of the given ids.
    /// </summary>
    /// <param name="ids">The ids of the stations.</param>
    /// <returns>A collection of stations with the given ids.</returns>
    Task<ICollection<Station>> GetStationsById(ICollection<Guid> ids);

    /// <summary>
    /// Update a station.
    /// </summary>
    /// <param name="station">The station to update.</param>
    Task Update(Station station);

    /// <summary>
    /// True if the station group for the given station is occupied by an agv.
    /// </summary>
    /// <param name="stationId">The id of the station.</param>
    Task<bool> IsStationGroupOccupied(Guid stationId);
}