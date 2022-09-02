using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Models
{
    /// <summary>
    /// class representing the Map table in Database
    /// </summary>
    public record Map : ISaga
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// MapId
        /// </summary>
        [UsedImplicitly]
        public string MapId { get; set; }

        /// <summary>
        /// Map in form bytes
        /// </summary>
        [UsedImplicitly]
        public byte[]? MapValue { get; set; }

        /// <summary>
        /// Timestamp of last modification
        /// </summary>
        [UsedImplicitly]
        public DateTime ModificationDate { get; set; }
        
        /// <summary>
        /// The resolution of the maps image. Expressed as how many [m] per pixel are represented
        /// </summary>
        public double Resolution { get; set; }
        
        /// <summary>
        /// The x offset of the map in [m] where the mapped graph coordinate system starts. . Origin is the left upper corner.
        /// </summary>
        public double XOffset { get; set; }
        
        /// <summary>
        /// The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.
        /// </summary>
        public double YOffset { get; set; }

        /// <summary>
        /// Full constructor
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="mapId"></param>
        /// <param name="mapValue"></param>
        /// <param name="resolution"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="modificationDate"></param>
        public Map(Guid correlationId, string mapId, byte[]? mapValue, double resolution, double xOffset, double yOffset, DateTime modificationDate)
        {
            CorrelationId = correlationId;
            MapId = mapId;
            MapValue = mapValue;
            Resolution = resolution;
            XOffset = xOffset;
            YOffset = yOffset;
            ModificationDate = modificationDate;
        }
    }
}
