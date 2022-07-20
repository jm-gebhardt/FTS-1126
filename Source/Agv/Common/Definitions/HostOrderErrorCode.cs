namespace Gebhardt.StoreWare.Agv.Common.Definitions
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
        /// Error code for order can not be modified or cancelled because it was already started
        /// </summary>
        OrderAlreadyStarted,
        /// <summary>
        /// Error code for order can not be modified or cancelled because it was already finished
        /// </summary>
        OrderAlreadyFinished
    }
}
