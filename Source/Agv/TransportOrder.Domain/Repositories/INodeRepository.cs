using Gebhardt.Acc.Common.Domain.Models.Graphs;

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
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<ICollection<(NodeLock NodeLock, Guid? StationId, string? LockReason, string? LockDescription, string? LockedByUser, string NodeName)>> GetNodeStatesForMapAsync(Guid mapCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a graph node.
    /// </summary>
    /// <param name="nodeId">The id of the node.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A graph node including its node lock.</returns>
    Task<GraphNode> GetNodeAsync(Guid nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the node lock.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="force">If true, update without any checks.</param>
    /// <param name="ignoreLocked">If true, ignore locked state of node.</param>
    ///<returns>True, if updating was successful.</returns>
    Task<bool> UpdateLock(GraphNode node, bool force = false, bool ignoreLocked = false);

    /// <summary>
    /// Get all nodes reserved for or occupied by given agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of nodes.</returns>
    Task<ICollection<GraphNode>> GetNodesForAgv(Guid agvCorrelationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get ids of nodes which are in state Locked.
    /// </summary>
    /// <returns></returns>
    public Task<IReadOnlyCollection<Guid>?> GetLockedNodes();
}