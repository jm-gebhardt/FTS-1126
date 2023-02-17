using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// Agv became ready
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvBecameReady(Guid CorrelationId) : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; } = CorrelationId;
    }
}
