using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.Statistics.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Domain.Repositories;

/// <summary>
/// Provides a repository for map grid cell related queries.
/// </summary>
public interface IMapGridCellRepository : IRepository<MapGridCell>
{
    /// <summary>
    /// Get all map grid cells based on map id.
    /// </summary>
    /// <returns>All <see cref="MapGridCell"/> related to the given map Id.</returns>
    [PublicAPI]
    Task<ICollection<MapGridCell>> GetMapAgvPositionsForMapAsync(Guid mapId);

    /// <summary>
    /// Updates the relationships (MapGridCell) between the map and its related grid cells.
    /// </summary>
    /// <param name="map">The map whose relationships to grid cells will be updated.</param>
    Task UpdateRelationsAsync(Map map);
}