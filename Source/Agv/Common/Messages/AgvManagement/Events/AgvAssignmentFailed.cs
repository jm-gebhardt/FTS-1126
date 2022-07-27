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
        /// The error code that holds information why the order failed
        /// </summary>
        public AgvAssignmentErrorCode Error { get; }

        /// <summary>
        /// AgvAssignmentFailed Constructor
        /// </summary>
        public AgvAssignmentFailed(Guid correlationId, AgvAssignmentErrorCode error)
        {
            CorrelationId = correlationId;
            Error = error;
        }
    }
    
    /// <summary>
    /// Reason why the assignment failed
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public enum AgvAssignmentErrorCode
    {
        /// <summary>
        /// There are no free AGVs to which this order could be assigned
        /// </summary>
        NoIdleAgv,
        /// <summary>
        /// There is no AGV registered with a route from source to destination of the given order
        /// </summary>
        NoRouteFound
    }
}
