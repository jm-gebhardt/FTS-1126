namespace Gebhardt.Acc.KaeserSidecar.Domain.Repositories;

/// <summary>
/// Provides host order related queries.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Store reporting of a pick at a station.
    /// </summary>
    /// <param name="station">The station.</param>
    /// <param name="orderId">The order id.</param>
    Task StorePickOrderReport(string station, Guid orderId);

    /// <summary>
    /// Remove report of a pick at a station.
    /// </summary>
    /// <param name="station">The station.</param>
    /// <param name="orderId">The order id.</param>
    Task RemovePickOrderReport(string station, Guid orderId);

    /// <summary>
    /// True, if the order for a pick at this station has been reported to the conveyors.
    /// </summary>
    /// <param name="station">The pick station</param>
    /// <param name="orderId">The order id.</param>
    /// <returns></returns>
    Task<bool> HasOrderBeenReported(string station, Guid orderId);
}