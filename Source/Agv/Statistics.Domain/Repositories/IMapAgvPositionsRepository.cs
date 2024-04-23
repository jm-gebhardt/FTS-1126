using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.Statistics.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for map positions related queries.
/// </summary>
public interface IMapAgvPositionsRepository : IRepository<MapAgvPositions>
{
    /// <summary>
    /// Get all map position based on map id.
    /// </summary>
    /// <returns>All <see cref="MapAgvPositions"/> related to the given map Id.</returns>
    [PublicAPI]
    Task<ICollection<MapAgvPositions>> GetMapAgvPositionsForMapAsync(Guid mapId);
}