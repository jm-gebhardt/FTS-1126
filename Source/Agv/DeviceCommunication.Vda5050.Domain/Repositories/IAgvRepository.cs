using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;

/// <summary>
/// Provides a repository for agv-related queries.
/// </summary>
public interface IAgvRepository
{
    /// <summary>
    /// Get an agv by its correlation id if available.
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns>The agv or null if none found.</returns>
    Task<Agv?> GetAgvAsync(Guid correlationId);

    /// <summary>
    /// Get an agv by its correlation id.
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns>The agv.</returns>
    /// <exception cref="AgvNotFoundException"></exception>
    Task<Agv> GetRequiredAgvAsync(Guid correlationId);

    /// <summary>
    /// Get all agvs.
    /// </summary>
    Task<IReadOnlyCollection<Agv>> GetAllAsync();

    /// <summary>
    /// Add or update an agv.
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddOrUpdateAsync(Agv agv, CancellationToken cancellationToken);

    /// <summary>
    /// Get agv by the id of the current order.
    /// </summary>
    /// <param name="orderId">The order id.</param>
    /// <returns>An agv.</returns>
    /// <exception cref="AgvNotFoundException"></exception>
    Task<Agv> GetAgvByCurrentOrderAsync(Guid orderId);

    /// <summary>
    /// Initialize the agv cache.
    /// </summary>
    Task InitializeCacheAsync();

    /// <summary>
    /// Get all agvs for given vehicle types.
    /// </summary>
    /// <param name="vehicleTypeIds">The vehicle types.</param>
    /// <returns>A collection of agvs.</returns>
    Task<ICollection<Agv>> GetAllForVehicleTypesAsync(ICollection<string> vehicleTypeIds);
}