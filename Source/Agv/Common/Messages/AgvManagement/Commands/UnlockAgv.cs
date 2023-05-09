using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands
{
    /// <summary>
    /// Request an AGV to lunock
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record UnlockAgv : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Build message.
        /// </summary>
        /// <param name="correlationId"></param>
        public UnlockAgv(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
