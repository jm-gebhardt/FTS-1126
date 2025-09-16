using Gebhardt.Acc.KaeserSidecar.Domain.Models;

namespace Gebhardt.Acc.KaeserSidecar.Domain.Repositories;

/// <summary>
/// Provides a repository for station-related requests.
/// </summary>
public interface IStationRepository
{
    /// <summary>
    /// Add agv to station.
    /// </summary>
    /// <param name="stationId">The Id of the station.</param>
    /// <param name="agv">The agv.</param>
    Task OccupyStation(Guid stationId, Guid agv);

    /// <summary>
    /// Free the station from an agv.
    /// </summary>
    /// <param name="stationId">The Id of the station.</param>
    Task FreeStation(Guid stationId);

    /// <summary>
    /// Get the station occupancy.
    /// </summary>
    Task<ICollection<StationOccupancy>> GetStationOccupancy();

    /// <summary>
    /// Get the occupancy of a station.
    /// </summary>
    /// <param name="stationId">The Id of the station.</param>
    Task<StationOccupancy> GetOccupancyOfStation(Guid stationId);

    /// <summary>
    /// Get the station an agv occupies.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <returns>The occupancy of the station the agv currently occupies, if any.</returns>
    Task<StationOccupancy?> GetOccupancyByAgvCorrelationId(Guid agvCorrelationId);
}