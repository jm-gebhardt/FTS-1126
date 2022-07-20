using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// AGV reservation request was successful
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public sealed class AgvReservationSucceeded : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// AgvReservationSucceeded Constructor
        /// </summary>
        /// <param name="correlationId"></param>
        public AgvReservationSucceeded(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
