using Gebhardt.Acc.TransportOrder.Domain.Models.Node;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for node-related queries across db tables.
/// </summary>
public interface INodeRepository
{
    /// <summary>
    /// Get states of all nodes on a given map.
    /// </summary>
    /// <param name="mapCorrelationId">The correlation id of the map.</param>
    Task<ICollection<(NodeLock NodeLock, Guid? StationId, string? LockReason, string? LockDescription, string? LockedByUser)>> GetNodeStatesForMapAsync(Guid mapCorrelationId);
}