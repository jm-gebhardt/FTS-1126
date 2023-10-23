using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;

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
    /// <returns>Agv object or null if no AGV with the given correlationId was found</returns>
    Task<Models.Agv.Agv> GetAgvAsync(Guid correlationId);

    /// <summary>
    /// Checks if the agv matching the correlation id exists.
    /// </summary>
    /// <param name="correlationId">The correlation id to check.</param>
    /// <returns>If the agv exists.</returns>
    Task<bool> CheckIfAgvExistsAsync(Guid correlationId);

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
    /// <returns></returns>
    Task<ICollection<Models.Agv.Agv>> GetReadyAgvsAsync();

    /// <summary>
    /// Get all charging agvs with good battery level.
    /// </summary>
    /// <param name="goodBatteryLevel">The good battery level.</param>
    /// <returns></returns>
    Task<ICollection<Models.Agv.Agv>> GetChargingAgvsWithGoodBatteryLevelAsync(int goodBatteryLevel);

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