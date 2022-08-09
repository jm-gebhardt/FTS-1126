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
        /// The Base64 encoded image data as string
        /// </summary>
        public string ImageData { get; }

        /// <summary>
        /// Build message
        /// </summary>
        /// <param name="correlationId">The correlation ID of the AGV</param>
        /// <param name="agvId">Identifier of the AGV.</param>
        /// <param name="mapCorrelationId">The correlation ID of the requested map</param>
        /// <param name="mapId">The human readable unique ID if the requested map</param>
        /// /// <param name="imageData">The base64 encoded image data as string</param>
        public AgvMapReceived(Guid correlationId, string agvId, Guid mapCorrelationId, string mapId, string imageData)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
            MapCorrelationId = mapCorrelationId;
            MapId = mapId;
            ImageData = imageData;
        }
    }

}
