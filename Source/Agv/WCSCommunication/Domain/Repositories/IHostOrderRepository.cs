using Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Models.HostOrder;

namespace Gebhardt.StoreWare.Agv.WCSCommunication.Domain.Repositories;

/// <summary>
/// Provides a repository for host order related queries.
/// </summary>
public interface IHostOrderRepository
{
    /// <summary>
    /// Add an order.
    /// </summary>
    /// <param name="hostOrder">The host order.</param>
    Task AddAsync(HostOrder hostOrder);

    /// <summary>
    /// Get an order.
    /// </summary>
    /// <param name="correlationId">The order correlation id.</param>
    /// <returns></returns>
    Task<HostOrder> GetByIdAsync(Guid correlationId);

    /// <summary>
    /// Remove an order.
    /// </summary>
    /// <param name="hostOrder">The host order.</param>
    Task RemoveAsync(HostOrder hostOrder);

    /// <summary>
    /// Update an order.
    /// </summary>
    /// <param name="hostOrder">The host order.</param>
    Task UpdateAsync(HostOrder hostOrder);
}