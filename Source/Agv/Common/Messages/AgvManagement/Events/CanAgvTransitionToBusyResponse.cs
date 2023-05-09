using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// Answer whether the Agv can go to busy
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public sealed class CanAgvTransitionToBusyResponse : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Can the Agv go to busy state?
        /// </summary>
        public bool Availability { get; }

        /// <summary>
        /// SetAgvBusySucceeded Constructor
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="availability"></param>
        public CanAgvTransitionToBusyResponse(Guid correlationId, bool availability)
        {
            CorrelationId = correlationId;
            Availability = availability;
        }
    }
}
