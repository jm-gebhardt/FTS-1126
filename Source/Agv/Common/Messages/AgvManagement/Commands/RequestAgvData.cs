using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands
{
    /// <summary>
    /// Command to request the data of an AGV.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record RequestAgvData : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Identifier of the AGV.
        /// </summary>
        public string AgvId { get; }

        /// <summary>
        /// Build message
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="agvId"></param>
        public RequestAgvData(Guid correlationId, string agvId)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
        }
    }
}
