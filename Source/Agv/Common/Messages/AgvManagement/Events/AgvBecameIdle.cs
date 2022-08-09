using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// Agv became idle
    /// </summary>
    public record AgvBecameIdle(Guid CorrelationId) : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; } = CorrelationId;
    }
}
