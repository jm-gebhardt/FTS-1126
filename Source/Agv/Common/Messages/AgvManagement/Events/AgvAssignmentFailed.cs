using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events
{
    /// <summary>
    /// AGV assignment failed, which usually means that there was no AGV available
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvAssignmentFailed : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// AgvAssignmentFailed Constructor
        /// </summary>
        /// <param name="correlationId"></param>
        public AgvAssignmentFailed(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
