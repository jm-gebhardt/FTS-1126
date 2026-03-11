using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

/// <summary>
/// The state of an order.
/// </summary>
[PublicAPI]
public class OrderState
{
    /// <summary>
    /// Timestamp in ISO8601 format (YYYY-MM-DDTHH:mm:ss.ffZ).
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Unique order identification of the current order or the previous finished order. The
    /// orderId is kept until a new order is received. Empty string ("") if no previous
    /// orderId is available.
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Order Update Identification to identify that an order
    /// update has been accepted by the AGV. 0 if no previous orderUpdateId is available.
    /// </summary>
    public int OrderUpdateId { get; set; }

    /// <summary>
    /// SequenceId of the last reached node or, if the AGV is currently on a node, sequenceId of
    /// current node.
    /// â€œ0â€ if no lastNodeSequenceId is available.
    /// </summary>
    public int LastNodeSequenceId { get; set; }

    /// <summary>
    /// True: AGV is almost at the end of the base and will reduce speed if no new base is transmitted.
    /// Trigger for master control to send new base.
    /// False: no base update required.
    /// </summary>
    public bool? NewBaseRequest { get; set; }

    /// <summary>
    /// Contains a list of the current actions and the actions which are yet to be finished.
    /// </summary>
    public IList<OrderActionState> ActionStates { get; set; } = [];

    /// <summary>
    /// Array of error-objects. All active errors of the AGV should be in the list.
    /// An empty array indicates that the AGV has no active errors.
    /// </summary>
    public IList<AgvError> Errors { get; set; } = [];

    /// <summary>
    /// Validation error.
    /// </summary>
    public AgvError? ValidationError => Errors.FirstOrDefault(e => e.ErrorType == Vda5050Specifications.OrderErrors.ValidationError);

    /// <summary>
    /// Update error.
    /// </summary>
    public AgvError? UpdateError => Errors.FirstOrDefault(e => e.ErrorType == Vda5050Specifications.OrderErrors.OrderUpdateError);

    /// <summary>
    /// Order error.
    /// </summary>
    public AgvError? OrderError => Errors.FirstOrDefault(e => e.ErrorType == Vda5050Specifications.OrderErrors.OrderError);
}