using JetBrains.Annotations;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Services;

/// <summary>
/// Provides a service to communicate with host system
/// that issues transport orders.
/// </summary>
public interface IHostCommunicator
{
    /// <summary>
    /// Report completion of an order to the host.
    /// </summary>
    /// <param name="hu">The handing unit.</param>
    /// <param name="orderId">The order id.</param>
    /// <param name="destination">The destination of arrival.</param>
    [PublicAPI]
    Task ReportOrderCompletion(string hu, string orderId, string destination);

    /// <summary>
    /// Report completion of a pick action to the host.
    /// </summary>
    /// <param name="hu">The handing unit.</param>
    /// <param name="position">The position of the pick</param>
    /// <param name="time">The time when the pick was completed.</param>
    void ReportPickCompletion(string hu, string position, DateTime time);

    /// <summary>
    /// Report error of transport order to the host.
    /// </summary>
    /// <param name="hu">The handing unit</param>
    /// <param name="source">The source of the transport order.</param>
    /// <param name="destination">The destination of the transport order.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorInfo">The error information.</param>
    /// <param name="time">The time the error was reported.</param>
    void ReportOrderError(string hu, string source, string destination, int errorCode, string errorInfo, DateTime time);
    
    /// <summary>
    /// Report system status.
    /// </summary>
    /// <param name="isReady">True, if the system is ready.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorInfo">The error information.</param>
    /// <param name="time">The time when the system status is reported.</param>
    void ReportSystemStatus(bool isReady, int errorCode, string errorInfo, DateTime time);
}