using Gebhardt.StoreWare.Agv.Common.Contracts;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// An event reporting the current status of an AGV.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvStatusReceived : CorrelatedBy<Guid>, IAgvId
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <inheritdoc />
        public string AgvId { get; }

        /// <summary>
        /// AgvStatus 
        /// </summary>
        public AgvStatus AgvStatus { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="AgvStatusReceived"/>
        /// </summary>
        public AgvStatusReceived(Guid correlationId, string agvId, AgvStatus agvStatus)
        {
            CorrelationId = correlationId;
            AgvId = agvId;
            AgvStatus = agvStatus;
        }
    }
}
