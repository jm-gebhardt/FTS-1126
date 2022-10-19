using Gebhardt.StoreWare.Agv.Common.Contracts;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// An event reporting the retrieval of AGV data.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvDataReceived : CorrelatedBy<Guid>, IAgvId
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <inheritdoc />
        public string AgvId { get; }
        /// <summary>
        /// Agv Graph
        /// </summary>
        public AgvData Data { get; }

        /// <summary>
        /// Build message.
        /// </summary>
        /// <param name="correlationId">Unique AGV Correlation ID</param>
        /// <param name="agvId">AGV serial number</param>
        /// <param name="data">AGV data</param>
        public AgvDataReceived(Guid correlationId, string agvId, AgvData data)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
            Data = data;
        }
    }

}
