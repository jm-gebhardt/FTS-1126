using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// Agv reservation request was rejected
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvReservationRejected : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// AgvReservationRejected Constructor
        /// </summary>
        /// <param name="correlationId"></param>
        public AgvReservationRejected(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
