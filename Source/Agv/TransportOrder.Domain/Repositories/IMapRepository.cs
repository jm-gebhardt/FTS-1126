using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using JetBrains.Annotations;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Abstracted interface for MapRepository.
/// </summary>
public interface IMapRepository
{
    /// <summary>
    /// Get all maps found in MapRepository
    /// </summary>
    public Task<ICollection<Map>> GetAllMapsAsync();

    /// <summary>
    /// Retrieves the background image for the given correlationId.
    /// </summary>
    /// <param name="correlationId">Unique identifier for the map background.</param>
    /// <param name="getBaseImage">If true, get the base map background image.</param>
    /// <returns>The custom background image (if there is any), or the base background</returns>
    Task<byte[]> GetBackgroundImageAsync(Guid correlationId, bool getBaseImage = false);

    /// <summary>
    /// /Retrieves the dimensions of the default map of the system.
    /// </summary>
    Task<(string? MapName, float MinY, float MaxY, float MinX, float MaxX)> GetDefaultMap();

    /// <summary>
    /// Sets (or overwrites) the custom background for a map.
    /// </summary>
    /// <param name="mapCorrelationId">The correlation id of the map.</param>
    /// <param name="mapBackground">The background to set.</param>
    Task SetCustomBackgroundAsync(Guid mapCorrelationId, MapBackground mapBackground);

    /// <summary>
    /// Delete the background associated with the given correlation ID.
    /// </summary>
    /// <param name="correlationId">CorrelationId of the background to delete.</param>
    Task<bool> DeleteCustomBackgroundAsync(Guid correlationId);

    /// <summary>
    /// Get information on a map.
    /// </summary>
    /// <param name="correlationId">The correlation id of the map.</param>
    Task<Map> GetMapAsync(Guid correlationId);

    /// <summary>
    /// Checks if map related to its correlationId exists in repository
    /// </summary>
    /// <param name="correlationId">GUID of the map looked for in the repository</param>
    [PublicAPI]
    public bool Exists(Guid correlationId);

    /// <summary>
    /// Add or update a map to/in the repository.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <param name="agvCorrelationId">The correlationId of the AGV which provides the map. </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the Map could be stored. False if e.g. the nodes/edges or Agvs were not existing.</returns>
    Task<bool> AddOrUpdateAsync(Map map, Guid agvCorrelationId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the background parameters of a map.
    /// </summary>
    /// <param name="correlationId">Unique identifier for the map.</param>
    /// <param name="resolution">The resolution of the maps image. Expressed as how many [m] per pixel are represented</param>
    /// <param name="xOffset">The x offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
    /// <param name="yOffset">The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.</param>
    Task UpdateMapBackgroundParametersAsync(Guid correlationId, double resolution, double xOffset, double yOffset);

    /// <summary>
    /// Adds the relation from the Agv to all Maps where the AgvGraph intersects with the nodes of the Map.
    /// </summary>
    /// <param name="agvId"></param>
    /// <returns></returns>
    Task AssignAgvGraphToMapsAsync(Guid agvId);

    /// <summary>
    /// Gets the correlation id of the map a node is on.
    /// </summary>
    /// <param name="nodeId">The node id.</param>
    /// <returns>Correlation id of a map.</returns>
    Task<Guid> GetMapIdForNodeAsync(Guid nodeId);
}