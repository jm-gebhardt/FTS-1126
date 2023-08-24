using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <summary>
/// Interface for accessing the AGV registry
/// </summary>
[JetBrains.Annotations.PublicAPI]
public interface IAgvRegistry
{
    /// <summary>
    /// Get count of all AGVs in the registry
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Get count of idle AGVs in the registry
    /// </summary>
    public int CountIdle { get; }

    /// <summary>
    /// Get list of all AGVs
    /// </summary>
    public IQueryable<IAgv> GetAllAgvs();

    /// <summary>
    /// Get list of AGVs that are ready to be assigned to an order
    /// </summary>
    public IQueryable<IAgv> GetIdleAgvs();

    /// <summary>
    /// Get specific AGV
    /// </summary>
    /// <returns>IAgv object or null if no AGV with the given correlationId was found</returns>
    public IAgv? GetAgv(Guid? correlationId);

    /// <summary>
    /// Get specific AGV
    /// </summary>
    /// <returns>IAgv object</returns>
    /// <exception cref="AgvNotFoundException"></exception>
    public IAgv GetRequiredAgv(Guid? correlationId);

    /// <summary>
    /// Checks if the agv matching the correlation id exists.
    /// </summary>
    /// <param name="correlationId">The correlation id to check.</param>
    /// <returns>If the agv exists.</returns>
    Task<bool> CheckIfAgvExistsAsync(Guid correlationId);
}