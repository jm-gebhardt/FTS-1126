using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands
{
    /// <summary>
    /// Command to request the assignment of an AGV for a specific order.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record RequestAgvGraph : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
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
