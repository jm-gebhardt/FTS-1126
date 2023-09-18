using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Map;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;

/// <summary>
/// Abstracted interface for MapRegistry
/// </summary>
public interface IMapRegistry
{
    /// <summary>
    /// Get all maps found in MapRegistry
    /// </summary>
    public IQueryable<Map> GetAllMaps();

    /// <summary>
    /// Get a map by its related correlationId from the MapRegistry
    /// </summary>
    /// <param name="correlationId">GUID of the map searched in the registry</param>
    [UsedImplicitly]
    public byte[]? GetMap(Guid correlationId);

    /// <summary>
    /// Checks if map related to its correlationId exists in registry
    /// </summary>
    /// <param name="correlationId">GUID of the map looked for in the registry</param>
    public bool Exists(Guid correlationId);

    /// <summary>
    /// Add or update a map to/in the registry.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if a change has been made</returns>
    Task<bool> AddOrUpdateAsync(Map map, CancellationToken cancellationToken);
}
