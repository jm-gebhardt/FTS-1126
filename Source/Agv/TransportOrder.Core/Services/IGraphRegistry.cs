using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models;
using System.Linq.Expressions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Abstracted interface for MapRegistry
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public interface IGraphRegistry
    {
        /// <summary>
        /// Gets the graph from the GraphRegistry (there is only one graph)
        /// </summary>
        public Graph GetGraph();

        /// <summary>
        /// Get nodes
        /// </summary>
        /// <param name="predicate">Optional Linq predicate</param>
        public IQueryable<Node> GetNodes(Expression<Func<Node, bool>>? predicate = null);

        /// <summary>
        /// Get edges
        /// </summary>
        /// <param name="predicate">Optional Linq predicate</param>
        public IQueryable<Edge> GetEdges(Expression<Func<Edge, bool>>? predicate = null);

        /// <summary>
        /// Add or update a single <see cref="Node"/> 
        /// </summary>
        /// <param name="node">Node</param>
        /// <returns>True if a changes could be stored, otherwise false</returns>
        public bool AddOrUpdateNode(Node node);

        /// <summary>
        /// Add or update a single <see cref="Edge"/>
        /// </summary>
        /// <param name="edge">Node</param>
        /// <returns>True if a changes could be stored, otherwise false</returns>
        public bool AddOrUpdateEdge(Edge edge);

        /// <summary>
        /// Update the whole graph from <see cref="AgvGraph"/>
        /// </summary>
        /// <param name="agvGraph">AGV graph</param>
        /// <returns>True if a changes could be stored, otherwise false</returns>
        public bool AddOrUpdateGraph(AgvGraph agvGraph);

        /// <summary>
        /// Update the whole graph
        /// </summary>
        /// <param name="agvGraph">AGV graph</param>
        /// <returns>True if a changes could be stored, otherwise false</returns>
        public bool AddOrUpdateGraph(Graph agvGraph);

        /// <summary>
        /// Sets the state of a node to 'Locked'
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns>True if the node could be freed</returns>
        public bool LockNode(string nodeId);

        /// <summary>
        /// Sets the state of a node to 'Free'
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns>True if the node could be freed</returns>
        public bool FreeNode(string nodeId);

        /// <summary>
        /// Sets the state of a node to 'Reserved' if the node is 'Free'
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns>True if the node could be reserved</returns>
        public bool ReserveNode(string nodeId);

        /// <summary>
        /// Checks if an AGV occupies a node based on the position of the AGV
        /// </summary>
        /// <param name="agvPositionStatus">Position status of AGV</param>
        /// <param name="previousOccupiedNode">Node that was previously occupied by the AGV</param>
        /// <param name="currentOccupiedNode">Node that is currently occupied by the AGV</param>
        /// <returns>True if a changes could be stored, otherwise false</returns>
        public bool UpdateOccupiedNode(AgvPositionStatus agvPositionStatus, out Node? previousOccupiedNode, out Node? currentOccupiedNode);

        /// <summary>
        /// Set occupied nodes based on positions of all AGVs
        /// </summary>
        /// <param name="allAgvPositions"></param>
        /// <returns>True if a changes could be stored, otherwise false</returns>
        public bool SetAllOccupiedNodes(List<AgvPositionStatus> allAgvPositions);
    }
}
