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
    /// <param name="station">The station.</param>
    /// <param name="agv">The agv.</param>
    void OccupyStation(string station, Guid agv);

    /// <summary>
    /// Free the station from an agv.
    /// </summary>
    /// <param name="station">The station</param>
    /// <param name="agv">The agv</param>
    void FreeStation(string station, Guid agv);

    /// <summary>
    /// Get the station occupancy.
    /// </summary>
    ICollection<StationOccupancy> GetStationOccupancy();
}