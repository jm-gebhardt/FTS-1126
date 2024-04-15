namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;

/// <summary>
/// Error code that can occur during handling of host orders
/// </summary>
public enum HostOrderHandlingFailedErrorCode
{
    /// <summary>
    /// Order already exists
    /// </summary>
    OrderAlreadyExists,

    /// <summary>
    /// Order data is invalid
    /// </summary>
    InvalidOrderData,

    /// <summary>
    /// Order failed by transport layer (e.g. AGV)
    /// </summary>
    FailedByTransportLayer
}