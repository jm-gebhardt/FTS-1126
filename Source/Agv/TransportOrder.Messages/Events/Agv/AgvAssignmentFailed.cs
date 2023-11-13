using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// AGV assignment failed, which usually means that there was no AGV available
/// </summary>
[PublicAPI]
public record AgvAssignmentFailed : CorrelatedBy<Guid>
{
    /// <summary>
    /// Reason why the assignment failed
    /// </summary>
    [PublicAPI]
    public enum ErrorCode
    {
        /// <summary>
        /// There are no free AGVs to which this order could be assigned
        /// </summary>
        NoReadyAgv,

        /// <summary>
        /// There is no AGV registered with a route from source to destination of the given order
        /// </summary>
        NoRouteFound,

        /// <summary>
        /// There was no AGV discovered or online when assigning the given order
        /// </summary>
        /// <remarks>No entry found in AgvRepository</remarks>
        AgvRepositoryEmpty
    }

    /// <inheritdoc/>
    public Guid CorrelationId { get; set;}

    /// <summary>
    /// The error code that holds information why assignment failed
    /// </summary>
    public ErrorCode Error { get; set; }

    /// <summary>
    /// AgvAssignmentFailed Constructor
    /// </summary>
    public AgvAssignmentFailed(Guid correlationId, ErrorCode error)
    {
        CorrelationId = correlationId;
        Error = error;
    }
}
