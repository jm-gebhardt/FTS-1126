using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;

/// <summary>
/// Define interface to access AGV position registry
/// </summary>
[PublicAPI]
public interface IAgvPositionRegistry
{
    /// <summary>
    /// Update position of an AGV
    /// </summary>
    /// <param name="agvPositionStatus">Position message from the AGV</param>
    Task UpdatePositionAsync(AgvPositionStatus agvPositionStatus);

    /// <summary>
    /// Get position of AGV
    /// </summary>
    /// <param name="correlationId">Correlation ID of the AGV</param>
    /// <returns>Returns the position of the requested AGV of null when no AGV with the given correlationId was found</returns>
    AgvPositionStatus? GetPosition(Guid correlationId);

    /// <summary>
    /// Returns a list position for all AGVs
    /// </summary>
    /// <returns>Returns a list position for all AGVs</returns>
    List<AgvPositionStatus> GetAllPositions();
}