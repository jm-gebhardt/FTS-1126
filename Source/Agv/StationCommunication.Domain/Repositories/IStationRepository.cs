using Gebhardt.Acc.StationCommunication.Domain.Models;

namespace Gebhardt.Acc.StationCommunication.Domain.Repositories;

/// <summary>
/// Provide a repository for station-related queries
/// </summary>
public interface IStationRepository
{
    /// <summary>
    /// Get a station by its name.
    /// </summary>
    /// <param name="name">The station name.</param>
    /// <returns>The station, if available.</returns>
    Task<Station?> GetStationByName(string name);
}