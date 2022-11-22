namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events
{
    /// <summary>
    /// Error code that can occur during handling of transport orders
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public enum TransportOrderErrorCode
    {
        /// <summary>
        /// Dummy value for any error
        /// </summary>
        Any,
        /// <summary>
        /// Error code that reports an abortion of the order by an user input
        /// </summary>
        AbortedByUser,
        /// <summary>
        /// Error code that reports a reset of the order by an user input
        /// </summary>
        ResetByUser
    }
}
