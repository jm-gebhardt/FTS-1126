using Gebhardt.StoreWare.Agv.TransportOrder.Common.Models;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Common.Services
{
    /// <summary>
    /// Abstracted interface for MapRegistry
    /// </summary>
    public interface IMapRegistry
    {
        /// <summary>
        /// Get all maps found in MapRegistry
        /// </summary>
        public IQueryable<IMap> GetAllMaps();

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
        /// Add or update a map to/in the registry
        /// </summary>
        /// <param name="correlationId">GUID of the map</param>
        /// <param name="base64PngImage"></param>
        /// <param name="mapId"></param>
        /// <param name="resolution"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="modificationDate"></param>
        public bool AddOrUpdate(Guid correlationId, string mapId, string base64PngImage, double resolution, double xOffset, double yOffset, DateTime modificationDate);
    }
}
