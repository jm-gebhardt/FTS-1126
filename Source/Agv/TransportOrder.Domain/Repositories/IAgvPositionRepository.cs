using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Define interface to access AGV position repository.
/// </summary>
[PublicAPI]
public interface IAgvPositionRepository
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
    AgvPositionStatus? GetPositionIfExisting(Guid correlationId);

    /// <summary>
    /// Get position of AGV
    /// </summary>
    /// <param name="correlationId">Correlation ID of the AGV</param>
    /// <returns>Returns the position of the requested AGV</returns>
    /// <exception cref="AgvLocationUnknownException">Thrown when AgvPosition is unknown</exception>
    AgvPositionStatus GetPosition(Guid correlationId);

    /// <summary>
    /// Returns positions of all agvs.
    /// </summary>
    /// <returns>Returns a list position for all AGVs</returns>
    ICollection<AgvPositionStatus> GetAllPositions();

    /// <summary>
    /// Get positions of selected agvs.
    /// </summary>
    /// <param name="correlationIds">The correlation ids of the selected agvs.</param>
    /// <returns></returns>
    ICollection<AgvPositionStatus> GetSelectedAgvsPositions(ICollection<Guid> correlationIds);
}