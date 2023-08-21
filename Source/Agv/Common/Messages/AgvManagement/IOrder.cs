using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

/// <summary>
/// IOrder is a read-only interface to provide current order data
/// </summary>
[JetBrains.Annotations.PublicAPI]
public interface IOrder : CorrelatedBy<Guid>
{
    /// <summary>
    /// Current State
    /// </summary>
    public string CurrentState { get; }

    /// <summary>
    /// AgvId
    /// </summary>
    public Guid? AssignedAgv { get; }

    /// <summary>
    /// The order details
    /// </summary>
    public OrderDetails OrderDetails { get; }

    /// <summary>
    /// Reason why an order failed
    /// </summary>
    /// <remarks>
    /// Primarily this is the event that leads to the failure (e.g. <see cref="Events.AgvAssignmentFailed"/>).
    /// When this event provides an error code it's appended to the event (e.g. <see cref="Events.AgvAssignmentFailed.ErrorCode.NoRouteFound"/>).
    /// </remarks>
    public string? FailedReason { get; set; }
}