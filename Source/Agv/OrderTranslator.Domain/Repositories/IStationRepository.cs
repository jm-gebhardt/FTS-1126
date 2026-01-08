using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.OrderTranslator.Domain.Models.Station;

namespace Gebhardt.Acc.OrderTranslator.Domain.Repositories;

/// <summary>
/// Provides a repository for station-related requests.
/// </summary>
public interface IStationRepository: IRepository<Station>
{
    /// <summary>
    /// Get all stations connected to the given conveyor lane.
    /// </summary>
    /// <param name="lane">The conveyor lane.</param>
    /// <returns>A collection of stations.</returns>
    Task<ICollection<Station>> GetStationsByLane(string lane);

    /// <summary>
    /// Get a station by its name.
    /// </summary>
    /// <param name="name">The station name.</param>
    /// <returns>The station.</returns>
    Task<Station?> GetStationByName(string name);
}