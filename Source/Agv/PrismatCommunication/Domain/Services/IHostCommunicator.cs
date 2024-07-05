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
}