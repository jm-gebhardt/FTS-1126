namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;

/// <summary>
/// Provides error code for failed host orders.
/// </summary>
public enum HostOrderCancelFailedErrorCode
{
    /// <summary>
    /// Order does not exist
    /// </summary>
    OrderDoesNotExist,

    /// <summary>
    /// Order has already been started
    /// </summary>
    OrderAlreadyStarted,

    /// <summary>
    /// Order already finished
    /// </summary>
    OrderAlreadyFinished,

    /// <summary>
    /// Order already finished with failure
    /// </summary>
    OrderAlreadyFailed,

    /// <summary>
    /// Order already canceled
    /// </summary>
    OrderAlreadyCanceled
}