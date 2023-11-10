using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;

/// <summary>
/// Abstracted interface for MapRepository.
/// </summary>
public interface IMapRepository
{
    /// <summary>
    /// Get all maps found in MapRepository
    /// </summary>
    public IQueryable<Map> GetAllMaps();

    /// <summary>
    /// Get a map by its related correlationId from the MapRepository
    /// </summary>
    /// <param name="correlationId">GUID of the map searched in the repository</param>
    [UsedImplicitly]
    public byte[]? GetMapImage(Guid correlationId);

    /// <summary>
    /// Get information on a map.
    /// </summary>
    /// <param name="correlationId">The correlation id of the map.</param>
    Task<Map> GetMapAsync(Guid correlationId);

    /// <summary>
    /// Checks if map related to its correlationId exists in repository
    /// </summary>
    /// <param name="correlationId">GUID of the map looked for in the repository</param>
    public bool Exists(Guid correlationId);

    /// <summary>
    /// Add or update a map to/in the repository.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if a change has been made</returns>
    Task<bool> AddOrUpdateAsync(Map map, CancellationToken cancellationToken);
}