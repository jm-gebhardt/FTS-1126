using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands
{
    /// <summary>
    /// Lock AGV to prevent it from being assigned to transport orders
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record LockAgv : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Build message.
        /// </summary>
        /// <param name="correlationId"></param>
        public LockAgv(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
