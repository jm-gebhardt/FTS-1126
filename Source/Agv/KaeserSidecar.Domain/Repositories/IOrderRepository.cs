namespace Gebhardt.Acc.KaeserSidecar.Domain.Repositories;

/// <summary>
/// Provides host order related queries.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Store reporting of an order.
    /// </summary>
    /// <param name="station">The station.</param>
    /// <param name="orderId">The order id.</param>
    Task StoreOrderReport(string station, Guid orderId);

    /// <summary>
    /// Remove report of an order.
    /// </summary>
    /// <param name="station">The station.</param>
    /// <param name="orderId">The order id.</param>
    Task RemoveOrderReport(string station, Guid orderId);

    /// <summary>
    /// True, if the order for this station has been reported to the conveyors.
    /// </summary>
    /// <param name="station">The station</param>
    /// <param name="orderId">The order id.</param>
    Task<bool> HasOrderBeenReported(string station, Guid orderId);
}