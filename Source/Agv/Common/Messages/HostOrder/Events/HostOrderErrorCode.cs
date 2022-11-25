namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events
{
    /// <summary>
    /// Error code that can occur during handling of host orders
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public enum HostOrderErrorCode
    {
        /// <summary>
        /// Dummy value for any error
        /// </summary>
        Any,
        /// <summary>
        /// Error code for order that can not be modified or cancelled because it was already started
        /// </summary>
        OrderAlreadyStarted,
        /// <summary>
        /// Error code for order that can not be modified or cancelled because it was already finished
        /// </summary>
        OrderAlreadyFinished,
        /// <summary>
        /// Error code for order that can not be modified or cancelled because it was already failed
        /// </summary>
        OrderAlreadyFailed,
        /// <summary>
        /// Error code for order that can not be modified or cancelled because it was already cancelled
        /// </summary>
        OrderAlreadyCancelled,
        /// <summary>
        /// Error code for an order that cannot be processed because its incoming order data was invalid
        /// </summary>
        InvalidOrderData
    }
}