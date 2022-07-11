using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands
{
    /// <summary>
    /// Command to request the assignment of an AGV for a specific order.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class RequestAgvGraph : CorrelatedBy<Guid>
    {
        /// <summary>
        /// Agv Correlation ID
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// AGV serial number.
        /// </summary>
        public string AgvId { get; }

        /// <summary>
        /// Build message.
        /// </summary>
        /// <param name="correlationId">Unique AGV Correlation ID</param>
        /// <param name="agvId">AGV serial number</param>
        public RequestAgvGraph(Guid correlationId, string agvId)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
        }
    }
}