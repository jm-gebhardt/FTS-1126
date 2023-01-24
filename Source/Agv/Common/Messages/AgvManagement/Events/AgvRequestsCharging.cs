using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// Event sent when an AGV requests to be moved to a charging station if it is not needed
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvRequestsCharging(Guid CorrelationId) : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; } = CorrelationId;
    }
}
