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
    /// Get a single station by name.
    /// </summary>
    /// <param name="name">The name of the station.</param>
    /// <returns>The station with the given name.</returns>
    Task<Station> GetStation(string name);

    /// <summary>
    /// Get all stations.
    /// </summary>
    /// <param name="mapName">Optional map name.</param>
    /// <returns>All stations (optionally on the given map).</returns>
    Task<ICollection<Station>> GetStations(string? mapName=null);
}