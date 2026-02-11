using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Node;

/// <summary>
/// Domain internal extensions for <see cref="GraphNode"/>
/// </summary>
public static class NodeExtension
{
    /// <summary>
    /// Try select a node from the collection by id
    /// </summary>
    /// <param name="nodes">The node collection.</param>
    /// <param name="nodeId">The node id.</param>
    /// <returns>The selected node.</returns>
    /// <exception cref="NodeNotFoundException">When the node is not found.</exception>
    public static GraphNode SelectById(this IEnumerable<GraphNode> nodes, Guid nodeId)
    {
        return nodes.FirstOrDefault(n => n.Id == nodeId) ?? throw new NodeNotFoundException(nodeId);
    }

    extension(GraphNode node)
    {
        /// <summary>
        /// True, if the there is no agv reserved for or occupying the node.
        /// </summary>
        public bool IsFree => node.Lock == null || node.Lock.NodeState == NodeLockState.Free;

        /// <summary>
        /// True, if the node is locked.
        /// </summary>
        public bool IsLocked => node.Lock is { IsLocked: true };

        /// <summary>
        /// True, if the node is occupied.
        /// </summary>
        public bool IsOccupied => node.Lock is { NodeState: NodeLockState.Occupied };

        /// <summary>
        /// True, if the node is reserved.
        /// </summary>
        public bool IsReserved => node.Lock is { NodeState: NodeLockState.Reserved };

        /// <summary>
        /// Reserve the node.
        /// </summary>
        /// <param name="agvCorrelationId">The correlation id of the agv.</param>
        /// <param name="force">If true, reserve without any checks.</param>
        /// <param name="ignoreLocked">Flag to reserve node even if it is locked.</param>
        /// <returns></returns>
        public bool Reserve(Guid agvCorrelationId, bool force = false, bool ignoreLocked=false)
        {
            if (force || ((node.Lock == null || node.IsFree) && (!node.IsLocked || ignoreLocked)))
            {
                node.Lock = new NodeLock(node, NodeLockState.Reserved, node.Lock?.IsLocked ?? false, agvCorrelationId, DateTime.UtcNow);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Occupy the node.
        /// </summary>
        /// <param name="agvCorrelationId">The correlation id of the agv.</param>
        /// <param name="force">If true, reserve without any checks.</param>
        /// <param name="ignoreLocked">Flag to occupy node even if it is locked.</param>
        /// <returns></returns>
        public bool Occupy(Guid agvCorrelationId, bool force = false, bool ignoreLocked = false)
        {
            if (force || ((node.Lock == null || node.IsReserved || node.IsFree) && (!node.IsLocked || ignoreLocked)))
            {
                node.Lock = new NodeLock(node, NodeLockState.Occupied, node.Lock?.IsLocked ?? false, agvCorrelationId, DateTime.UtcNow);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Free a node.
        /// </summary>
        /// <returns>True, if freeing was successful.</returns>
        public bool Free()
        {
            if (node.IsFree)
            {
                return true;
            }
            
            node.Lock = new NodeLock(node, NodeLockState.Free, node.Lock?.IsLocked ?? false, null, DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// Lock a node.
        /// </summary>
        /// <returns>True, if locking was successful.</returns>
        public bool Lock()
        {
            if (node.Lock?.IsLocked ?? false)
            {
                return false;
            }
            node.Lock = new NodeLock(node, node.Lock?.NodeState ?? NodeLockState.Free, true, node.Lock?.AgvCorrelationId, DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// Unlock a node.
        /// </summary>
        /// <returns>True, if unlocking was successful.</returns>
        public bool Unlock()
        {
            if (node.Lock is not { IsLocked: true })
            {
                return false;
            }
            node.Lock = new NodeLock(node, node.Lock?.NodeState ?? NodeLockState.Free, false, node.Lock?.AgvCorrelationId, DateTime.UtcNow);
            return true;
        }
    }
}