using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Interface for accessing the AGV repository.
/// </summary>
[PublicAPI]
public interface IAgvRepository
{
    /// <summary>
    /// Get count of all AGVs
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Get list of all AGVs
    /// </summary>
    Task<ICollection<Models.Agv.Agv>> GetAllAgvsAsync();

    /// <summary>
    /// Get list of all AGVs where the graph exists
    /// </summary>
    Task<ICollection<Models.Agv.Agv>> GetAllAgvsWithGraphAsync();

    /// <summary>
    /// Get list of AGVs that are ready to be assigned to an order
    /// </summary>
    Task<ICollection<Models.Agv.Agv>> GetIdleAgvsAsync();

    /// <summary>
    /// Get specific AGV
    /// </summary>
    /// <returns>Agv object.</returns>
    /// <exception cref="AgvNotFoundException">If the agv with the given correlation id does not exist.</exception>
    Task<Models.Agv.Agv> GetAgvAsync(Guid correlationId);

    /// <summary>
    /// Get specific AGV
    /// </summary>
    /// <returns>Agv object or null.</returns>
    Task<Models.Agv.Agv?> TryGetAgvAsync(Guid correlationId);

    /// <summary>
    /// Checks if the agv matching the correlation id exists.
    /// </summary>
    /// <param name="correlationId">The correlation id to check.</param>
    /// <returns>If the agv exists.</returns>
    Task<bool> CheckIfAgvExistsAsync(Guid correlationId);

    /// <summary>
    /// Throws an <see cref="AgvNotFoundException"/> if the agv with the given correlation id does not exist.
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the Agv.</param>
    /// <returns></returns>
    Task ThrowIfAgvNotExistsAsync(Guid correlationId);

    /// <summary>
    /// Throws an <see cref="AgvNotFoundException"/> for all of the non existing agvs.
    /// </summary>
    /// <param name="correlationIds">The CorrelationIds of the Agv to check.</param>
    /// <returns></returns>
    Task ThrowIfAgvsNotExistAsync(ICollection<Guid> correlationIds);

    /// <summary>
    /// Select from the given agv ids the ones that exists.
    /// </summary>
    Task<ICollection<Models.Agv.Agv>> SelectExistingAgvsAsync(List<Guid> agvCorrelationIds);

    /// <summary>
    /// Update an agv.
    /// </summary>
    /// <param name="agv">The agv.</param>
    Task<bool> UpdateAgvAsync(Models.Agv.Agv agv);

    /// <summary>
    /// Add an agv.
    /// </summary>
    /// <param name="agv">The agv.</param>
    Task<bool> AddAgvAsync(Models.Agv.Agv agv);

    /// <summary>
    /// Get all ready agvs.
    /// </summary>
    /// <param name="minBatteryLevel">The Agv must have at least this battery level to be contained.</param>
    /// <returns></returns>
    Task<ICollection<Models.Agv.Agv>> GetReadyAgvsAsync(int minBatteryLevel = 0);

    /// <summary>
    /// Gets all agvs that have a battery level higher than their specified low battery level.
    /// </summary>
    /// <returns>Agvs.</returns>
    Task<ICollection<Models.Agv.Agv>> GetReadyAgvsWithAtLeastLowBatteryLevel();

   /// <summary>
   /// Get all charging agvs with at least their MinChargeUp battery level.
   /// </summary>
   Task<ICollection<Models.Agv.Agv>> GetChargingAgvsWithMinBatteryLevelAsync();

    /// <summary>
    /// Get the last node id of the agv with the given correlation id.
    /// </summary>
    /// <param name="agvCorrelationId">The AgvCorrelationId</param>
    /// <returns></returns>
    Task<string?> GetLastNodeIdAsync(Guid agvCorrelationId);

    /// <summary>
    /// Get the last node id of the agvs with the given correlation ids.
    /// </summary>
    /// <param name="agvCorrelationIds">A list of AgvCorrelationIds</param>
    /// <returns></returns>
    Task<IReadOnlyDictionary<Guid, string?>> GetLastNodeIdAsync(ICollection<Guid> agvCorrelationIds);
}