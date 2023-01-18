using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// An event reporting that an AGV has started charging
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvStartedCharging : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Initializing new Instance of <see cref="AgvStartedCharging"/>
        /// </summary>
        /// <param name="correlationId"></param>
        public AgvStartedCharging(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
