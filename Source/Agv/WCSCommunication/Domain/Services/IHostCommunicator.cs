namespace Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Services;

/// <summary>
/// Provides a service to communicate with host system
/// that issues transport orders.
/// </summary>
public interface IHostCommunicator
{
    /// <summary>
    /// Transmit host order handling succeeded to the host
    /// </summary>
    Task<bool> TransmitHostOrderHandlingSucceeded(string orderId);

    /// <summary>
    /// Transmit host order start to the host
    /// </summary>
    Task<bool> TransmitHostOrderStarted(string orderId, string assignedAgv);

    /// <summary>
    /// Transmit host order handling failed to the host
    /// </summary>
    Task<bool> TransmitHostOrderHandlingFailure(string orderId, string error);

    /// <summary>
    /// Transmit host order cancellation failed to the host
    /// </summary>
    Task<bool> TransmitHostOrderCancellationFailure(string orderId, string error);

    /// <summary>
    /// Transmit host order cancellation succeeded to the host
    /// </summary>
    Task<bool> TransmitHostOrderCancellationSucceeded(string orderId);

}