using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
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
    Task<ICollection<Agv>> GetAllAgvsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of all AGVs where the graph exists
    /// </summary>
    Task<ICollection<Agv>> GetAllAgvsWithGraphAsync();

    /// <summary>
    /// Get list of AGVs that are ready to be assigned to an order
    /// </summary>
    Task<ICollection<Agv>> GetIdleAgvsAsync();

    /// <summary>
    /// Get specific AGV
    /// </summary>
    /// <param name="correlationId">The correlation id of the AGV.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agv object.</returns>
    /// <exception cref="AgvNotFoundException">If the agv with the given correlation id does not exist.</exception>
    Task<Agv> GetAgvAsync(Guid correlationId, CancellationToken cancellationToken=default);

    /// <summary>
    /// Get an online AGV.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An Agv object.</returns>
    Task<Agv?> GetAnyOnlineAgv(CancellationToken cancellationToken=default);

    /// <summary>
    /// Get specific AGV
    /// </summary>
    /// <param name="correlationId">The correlation id of the AGV.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agv object or null.</returns>
    Task<Agv?> TryGetAgvAsync(Guid correlationId, CancellationToken cancellationToken=default);

    /// <summary>
    /// Checks if the agv matching the correlation id exists.
    /// </summary>
    /// <param name="correlationId">The correlation id to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>If the agv exists.</returns>
    Task<bool> CheckIfAgvExistsAsync(Guid correlationId, CancellationToken cancellationToken=default);

    /// <summary>
    /// Throws an <see cref="AgvNotFoundException"/> if the agv with the given correlation id does not exist.
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the Agv.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task ThrowIfAgvNotExistsAsync(Guid correlationId, CancellationToken cancellationToken=default);

    /// <summary>
    /// Throws an <see cref="AgvNotFoundException"/> for all not existing agvs.
    /// </summary>
    /// <param name="correlationIds">The CorrelationIds of the Agv to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task ThrowIfAgvsNotExistAsync(ICollection<Guid> correlationIds, CancellationToken cancellationToken=default);

    /// <summary>
    /// Select from the given agv ids the ones that exists.
    /// </summary>
    Task<ICollection<Agv>> SelectExistingAgvsAsync(List<Guid> agvCorrelationIds, CancellationToken cancellationToken=default);

    /// <summary>
    /// Selects all agvs that have above critical battery level.
    /// </summary>
    /// <param name="agvCorrelationIds">The agvs to select from.</param>
    /// <returns></returns>
    Task<ICollection<Agv>> SelectAgvsWithAboveCriticalBatteryLevel(ICollection<Guid> agvCorrelationIds);

    /// <summary>
    /// Selects all agvs that have above min charge battery level and that are configured for infinite charging.
    /// </summary>
    /// <param name="agvCorrelationIds">The agvs to select from.</param>
    /// <returns></returns>
    Task<ICollection<Agv>> SelectInfinitelyChargingAgvsWithAboveMinChargeUpBatteryLevel(ICollection<Guid> agvCorrelationIds);

    /// <summary>
    /// Update an agv.
    /// </summary>
    /// <param name="agv">The agv.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> UpdateAgvAsync(Agv agv, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add an agv.
    /// </summary>
    /// <param name="agv">The agv.</param>
    Task<bool> AddAgvAsync(Agv agv);

    /// <summary>
    /// Gets all agvs that have a battery level higher than their specified low battery level.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agvs.</returns>
    Task<ICollection<Agv>> GetReadyAgvsWithAtLeastLowBatteryLevel(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all charging agvs with at least their MinChargeUp battery level.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ICollection<Agv>> GetChargingAgvsWithMinBatteryLevelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all agvs with critical battery level that are not currently charging.
    /// </summary>
    public Task<ICollection<Agv>> GetNotChargingAgvsWithCriticalBatteryLevel();

    /// <summary>
    /// Get the last node id of the agv with the given correlation id.
    /// </summary>
    /// <param name="agvCorrelationId">The AgvCorrelationId</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<Guid?> GetLastNodeIdAsync(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the last node id of the agvs with the given correlation ids.
    /// </summary>
    /// <param name="agvCorrelationIds">A list of AgvCorrelationIds</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns></returns>
    Task<IReadOnlyDictionary<Guid, Guid?>> GetLastNodeIdAsync(ICollection<Guid> agvCorrelationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// True, if there are online agvs.
    /// </summary>
    Task<bool> HasOnlineAgvs();

    /// <summary>
    /// Gets all the agvs currently on a charger with min charge level.
    /// </summary>
    /// <returns>Charging agvs with at least minimum charge up level.</returns>
    Task<ICollection<Agv>> GetAgvsOnChargerWithAtLeastMinChargeUpLevel();

    /// <summary>
    /// Gets all the agvs currently on a charger with at least the specified level.
    /// </summary>
    /// <returns>Charging agvs with at least the specified level.</returns>
    Task<ICollection<Agv>> GetAgvsOnChargerWithAtLeastChargeUpLevel(double chargeLevel);
}