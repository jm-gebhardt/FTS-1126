using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands
{
    /// <summary>
    /// Request an AGV to be reserved
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record ReserveAgv : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Build message.
        /// </summary>
        /// <param name="correlationId"></param>
        public ReserveAgv(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
