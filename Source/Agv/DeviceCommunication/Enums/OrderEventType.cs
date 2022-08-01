using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Enums
{
    /// <summary>
    /// Order event types for of a transport order (see 'order_event.schema')
    /// </summary>
    [PublicAPI]
    public enum OrderEventType
    {
        /// <summary>
        /// Transport order started
        /// </summary>
        Started,
        /// <summary>
        /// Pick for transport order completed
        /// </summary>
        Picked,
        /// <summary>
        /// Transport order completed
        /// </summary>
        Completed,
        /// <summary>
        /// Transport order cancelled
        /// </summary>
        Cancelled,
        /// <summary>
        /// Cancellation for transport order failed
        /// </summary>
        CancelFailed,
        /// <summary>
        /// Transport order updated
        /// </summary>
        Updated,
        /// <summary>
        /// Transport order update failed
        /// </summary>
        UpdateFailed,
        /// <summary>
        /// Transport order Failed
        /// </summary>
        Failed
    }
}
