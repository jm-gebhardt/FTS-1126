using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands
{
    /// <summary>
    /// Command to request the map of an AGV.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record RequestAgvMap : CorrelatedBy<Guid>
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
        /// Build message
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="agvId"></param>
        /// <param name="mapCorrelationId"></param>
        /// <param name="mapId"></param>
        public RequestAgvMap(Guid correlationId, string agvId, Guid mapCorrelationId, string mapId)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
            MapCorrelationId = mapCorrelationId;
            MapId = mapId;
        }

    }
}
