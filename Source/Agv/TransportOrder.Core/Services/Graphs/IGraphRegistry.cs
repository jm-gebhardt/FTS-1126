using System.Linq.Expressions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs
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
        /// <param name="agvs">Only nodes related to the provided agvs are returned. If null all nodes will be returned</param>
        public IQueryable<Node> GetNodes(Expression<Func<Node, bool>>? predicate = null, IEnumerable<IAgv>? agvs = null);

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
    }
}
