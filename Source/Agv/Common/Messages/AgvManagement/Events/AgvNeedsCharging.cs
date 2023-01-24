using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// Event sent when an AGV needs to be moved to a charging station
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvNeedsCharging(Guid CorrelationId) : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; } = CorrelationId;
    }
}
