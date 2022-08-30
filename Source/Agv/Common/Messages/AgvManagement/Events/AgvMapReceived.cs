using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// An event reporting the retrieval of an AGV map.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvMapReceived : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Identifier of the AGV.
        /// </summary>
        public string AgvId { get; }

        /// <summary>
        /// The correlation ID of the requested map
        /// </summary>
        public Guid MapCorrelationId { get; }

        /// <summary>
        /// The human readable unique ID if the requested map
        /// </summary>
        public string MapId { get; }

        /// <summary>
        /// Timestamp of last modification
        /// </summary>
        public DateTime ModificationDate { get; }

        /// <summary>
        /// The Base64 encoded image data as string
        /// </summary>
        public string ImageData { get; }

        /// <summary>
        /// Resolution of the Image in [m/px].
        /// </summary>
        public float Resolution { get; }

        /// <summary>
        /// Distance from the left bound to the origin of the graph coordinates.
        /// When the origin is left from the left bound this value is negative.
        /// </summary>
        public float XOffset { get; }

        /// <summary>
        /// Distance from the top bound to the origin of the graph coordinates.
        /// When the origin is upper to the top bound this value is negative.
        /// </summary>
        public float YOffset { get; }

        /// <summary>
        /// Build message
        /// </summary>
        /// <param name="correlationId">The correlation ID of the AGV</param>
        /// <param name="agvId">Identifier of the AGV.</param>
        /// <param name="mapCorrelationId">The correlation ID of the requested map</param>
        /// <param name="mapId">The human readable unique ID if the requested map</param>
        /// <param name="modificationDate">Timestamp of last modification.</param>
        /// <param name="imageData">The base64 encoded image data as string</param>
        /// <param name="resolution">Resolution of the Image in [m/px].</param>
        /// <param name="xOffset">Distance from the left bound to the origin of the graph coordinates.</param>
        /// <param name="yOffset">Distance from the top bound to the origin of the graph coordinates.</param>
        public AgvMapReceived(Guid correlationId, string agvId, Guid mapCorrelationId, string mapId, DateTime modificationDate, string imageData,
                              float resolution, float xOffset, float yOffset)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
            MapCorrelationId = mapCorrelationId;
            MapId = mapId;
            ModificationDate = modificationDate;
            ImageData = imageData;
            Resolution = resolution;
            XOffset = xOffset;
            YOffset = yOffset;
        }
    }
}
