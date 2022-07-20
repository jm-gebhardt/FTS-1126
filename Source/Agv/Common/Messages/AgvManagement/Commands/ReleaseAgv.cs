using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands
{
    /// <summary>
    /// Moves agv into idle state
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record ReleaseAgv : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// Build message.
        /// </summary>
        /// <param name="correlationId"></param>
        public ReleaseAgv(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
