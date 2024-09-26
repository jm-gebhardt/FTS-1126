using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Interface for locking nodes 
/// </summary>
public interface INodeStateRepository
{
    /// <summary>
    /// Sets the state of a node to 'Locked'
    /// </summary>
    /// <param name="nodeId"></param>
    /// <returns>True if the node could be freed</returns>
    public Task<bool> LockNodeAsync(string nodeId);

    /// <summary>
    /// Sets the state of a node to 'unlocked'
    /// </summary>
    /// <param name="nodeId"></param>
    /// <returns>True if the node could be freed</returns>
    public Task<bool> UnlockNodeAsync(string nodeId);

    /// <summary>
    /// Reserve a node for an agv
    /// Node can not be reserved when locked
    /// </summary>
    /// <param name="nodeId"></param>
    /// <param name="agvCorrelationId"></param>
    /// <returns></returns>
    public Task<bool> ReserveNodeAsync(string nodeId, Guid agvCorrelationId);

    /// <summary>
    /// Un-reserve a node.
    /// If the node is locked, it will not be un-reserved.
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    [UsedImplicitly]
    public Task<bool> UnReserveNodeAsync(Guid agvCorrelationId);

    /// <summary>
    /// Occupy a node
    /// </summary>
    /// <param name="nodeId"></param>
    /// <param name="agvCorrelationId"></param>
    /// <returns></returns>
    public Task<bool> OccupyNodeAsync(string nodeId, Guid agvCorrelationId);

    /// <summary>
    /// Free an occupied node.
    /// If the node is locked, it will not be freed.
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <returns></returns>
    public Task<bool> UnOccupyNodeAsync(Guid agvCorrelationId);

    /// <summary>
    /// Free a reserved or occupied node.
    /// If the node is locked, it will not be freed.
    /// </summary>
    /// <param name="nodeId"></param>
    /// <returns></returns>
    [UsedImplicitly]
    public Task<bool> FreeNodeAsync(string nodeId);

    /// <summary>
    /// Is node reserved
    /// </summary>
    /// <returns></returns>
    public Task<bool> IsNodeReservedAsync(string nodeId);

    /// <summary>
    /// Get free and unlocked nodes
    /// </summary>
    /// <returns></returns>
    public IEnumerable<GraphNode> FilterFreeUnlockedNodes(IEnumerable<GraphNode> nodes);

    /// <summary>
    /// Get state of node.
    /// </summary>
    /// <param name="nodeId">The node id.</param>
    /// <returns></returns>
    Task<NodeLock?> GetNodeStateAsync(string nodeId);

    /// <summary>
    /// Checks if any of the given nodes is reserved for the given agv.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="nodes">The nodes in question.</param>
    /// <returns></returns>
    Task<bool> IsAnyNodeReservedForAgv(Guid agvCorrelationId, ICollection<string> nodes);

    /// <summary>
    /// Gets a reserved NodeLock for an AGV and the specified collection of nodes.
    /// </summary>
    /// <param name="agvCorrelationId">AGV correlation ID.</param>
    /// <param name="nodes">Collection of nodes to check</param>
    /// <returns></returns>
    public Task<NodeLock?> GetReservedNodeLockForAgv(Guid agvCorrelationId, ICollection<string> nodes);

    /// <summary>
    /// Get ids of nodes which are in state Locked.
    /// </summary>
    /// <returns></returns>
    public Task<IReadOnlyCollection<string>?> GetLockedNodes();

    /// <summary>
    /// Get all node locks that are concerning the given agv.
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <returns>Collection of node locks.</returns>
    public Task<ICollection<NodeLock>> GetNodeLocksForAgv(Guid agvCorrelationId);
}