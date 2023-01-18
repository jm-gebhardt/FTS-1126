using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// An event reporting that an AGV has stopped charging
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvStoppedCharging : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Initializing new Instance of <see cref="AgvStoppedCharging"/>
        /// </summary>
        /// <param name="correlationId"></param>
        public AgvStoppedCharging(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}