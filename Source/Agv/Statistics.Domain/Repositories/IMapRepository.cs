using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.Statistics.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Domain.Models;

namespace Gebhardt.Acc.Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for map related queries.
/// </summary>
public interface IMapRepository : IRepository<Map>
{
    /// <summary>
    /// Get map for a given grid if existing.
    /// </summary>
    Task<Map?> GetMapAsync(Guid mapId);

    /// <summary>
    /// Throw <see cref="MapNotFoundException"/> if the map does not exist.
    /// </summary>
    /// <param name="mapId">The id of the map.</param>
    /// <exception cref="MapNotFoundException">Thrown, if the map does not exist.</exception>>
    Task ThrowIfNotExistAsync(Guid mapId);
}