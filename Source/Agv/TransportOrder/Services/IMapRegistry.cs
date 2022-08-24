using Gebhardt.StoreWare.Agv.TransportOrder.Persistence;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Abstracted interface for MapRegistry
    /// </summary>
    public interface IMapRegistry
    {
        /// <summary>
        /// Get all maps found in MapRegistry
        /// </summary>
        public IEnumerable<Map> GetAllMaps();

        /// <summary>
        /// Get a map by its related correlationId from the MapRegistry
        /// </summary>
        /// <param name="correlationId">GUID of the map searched in the registry</param>
        [JetBrains.Annotations.UsedImplicitly]
        public byte[]? GetMap(Guid correlationId);

        /// <summary>
        /// Checks if map related to its correlationId exists in registry
        /// </summary>
        /// <param name="correlationId">GUID of the map looked for in the registry</param>
        public bool Exists(Guid correlationId);

        /// <summary>
        /// Add a new map to the registry
        /// </summary>
        /// <param name="correlationId">GUID of the map added to the registry</param>
        /// <param name="base64PngImage"></param>
        /// <param name="mapId"></param>
        public bool Add(Guid correlationId, string base64PngImage, string mapId);

        /// <summary>
        /// Update image of a given map in the registry
        /// </summary>
        /// <param name="correlationId">GUID of the map updated in the registry</param>
        /// <param name="base64PngImage"></param>
        public bool Update(Guid correlationId, string base64PngImage);
    }
}
