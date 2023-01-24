using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// Event signaling that an Agv is ready to handle transport orders
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvReady(Guid CorrelationId) : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; } = CorrelationId;
    }
}
