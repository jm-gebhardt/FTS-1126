using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.Web.Models
{
    /// <summary>
    /// Map Model
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record Map : CorrelatedBy<Guid>
    {
        /// <summary>
        /// Map Correlation Id
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Map Id
        /// </summary>
        public string MapId { get; }
        
        /// <summary>
        /// The resolution of the maps image. Expressed as how many [m] per pixel are represented
        /// </summary>
        public double Resolution { get; }
        
        /// <summary>
        /// The x offset of the map in [m] where the mapped graph coordinate system starts. . Origin is the left upper corner.
        /// </summary>
        public double XOffset { get; }
        
        /// <summary>
        /// The y offset of the map in [m] where the mapped graph coordinate system starts. Origin is the left upper corner.
        /// </summary>
        public double YOffset { get; }
        
        /// <summary>
        /// List of Agvs using this map
        /// </summary>
        public Guid[] RelatedAgvs { get; }

        /// <summary>
        /// Map Constructor
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="mapId"></param>
        /// <param name="resolution"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="relatedAgvs"></param>
        public Map(Guid correlationId, string mapId, double resolution, double xOffset, double yOffset,  Guid[] relatedAgvs)
        {
            CorrelationId = correlationId;
            MapId = mapId;
            Resolution = resolution;
            XOffset = xOffset;
            YOffset = yOffset;
            RelatedAgvs = relatedAgvs;
        }
    }
}