using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence
{
    /// <summary>
    /// class representing the Map table in Database
    /// </summary>
    public class Map : ISaga
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
        public DateTime ModificationDateTime { get; set; }

        
        /// <summary>
        /// Full constructor
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="mapId"></param>
        /// <param name="mapValue"></param>
        /// <param name="modificationDateTime"></param>
        public Map(Guid correlationId, string mapId, byte[]? mapValue, DateTime modificationDateTime)
        {
            CorrelationId = correlationId;
            MapId = mapId;
            MapValue = mapValue;
            ModificationDateTime = modificationDateTime;
        }
    }
}
