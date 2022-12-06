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
        /// <summary>
        /// Reason why the assignment failed
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public enum ErrorCode
        {
            /// <summary>
            /// There are no free AGVs to which this order could be assigned
            /// </summary>
            NoIdleAgv,

            /// <summary>
            /// There is no AGV registered with a route from source to destination of the given order
            /// </summary>
            NoRouteFound,

            /// <summary>
            /// There was no AGV discovered or online when assigning the given order
            /// </summary>
            /// <remarks>No entry found in AgvRegistry</remarks>
            AgvRegistryEmpty
        }


        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// The error code that holds information why assignment failed
        /// </summary>
        public ErrorCode Error { get; }

        /// <summary>
        /// AgvAssignmentFailed Constructor
        /// </summary>
        public AgvAssignmentFailed(Guid correlationId, ErrorCode error)
        {
            CorrelationId = correlationId;
            Error = error;
        }
    }
}
