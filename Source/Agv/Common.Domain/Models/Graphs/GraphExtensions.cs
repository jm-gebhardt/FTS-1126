namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
/// Provides extension methods for <see cref="IGraph"/>.
/// </summary>
public static class GraphExtensions
{
    /// <summary>
    /// Extension block for <see cref="IGraph"/>.
    /// </summary>
    extension(IGraph graph)
    {
        /// <summary>
        /// Validates the graph.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void Validate()
        {
            var duplicateEdges = graph.Edges.GroupBy(e => e.Id).Where(g => g.Count() > 1).ToList();
            if (duplicateEdges.Count != 0)
            {
                throw new ArgumentException("Duplicate Edges. Edge Ids must be unique.");
            }

            var duplicateNodes = graph.Nodes.GroupBy(n => n.Id).Where(g => g.Count() > 1).ToList();
            if (duplicateNodes.Count != 0)
            {
                throw new ArgumentException("Duplicate Nodes. Node Ids must be unique.");
            }
        }

        /// <summary>
        /// Get the length [m] of a route given by node ids.
        /// </summary>
        /// <param name="routeNodes">The route (collection of node ids).</param>
        /// <returns>Length of the route [m].</returns>
        /// <exception cref="ArgumentNullException">Ambiguous route.</exception>
        /// <exception cref="InvalidOperationException">If any of the subsequent nodes do not match an edge.</exception>
        public double GetRouteLength(GraphNode[] routeNodes)
        {
            double totalLength = 0.0;
            for (int i = 0; i < routeNodes.Length - 1; i++)
            {
                totalLength += graph.Edges.Single(edge => edge.FromNodeId == routeNodes[i].Id && edge.ToNodeId == routeNodes[i + 1].Id).Length;
            }
            return totalLength;
        }

        /// <summary>
        /// Get the length [m] of a route given by edge ids.
        /// </summary>
        /// <param name="routeEdges">The route (collection of edge ids).</param>
        /// <param name="lastNodeId">The id of the last node passed.</param>
        /// <returns>Length of the route [m].</returns>
        /// <exception cref="ArgumentNullException">Ambiguous route.</exception>
        /// <exception cref="InvalidOperationException">If any of the subsequent nodes do not match an edge.</exception>
        public double GetRouteLength(Guid[] routeEdges, Guid? lastNodeId)
        {
            double totalLength = 0.0;
            var lastEdge = graph.Edges.Where(e => e.ToNodeId == lastNodeId).Select(e => e.Id).ToList(); 
            foreach(var edgeId in routeEdges)
            {
                totalLength += graph.Edges.SingleOrDefault(edge => edge.Id == edgeId)?.Length ?? 0;
                if (lastEdge.Contains(edgeId))
                {
                    break;
                }
            }
            return totalLength;
        }

        /// <summary>
        /// Get a collection of arbitrary neighboring edges (for unit testing).
        /// <param name="count">The number of edges.</param>
        /// </summary>
        /// <returns>A collection of neighboring edges.</returns>
        public ICollection<GraphEdge> GetNeighboringEdges(int count)
        {
            if (graph.Edges.Count < count)
            {
                throw new Exception($"Graph contains less then {count} edges");
            }
            List<GraphEdge> neighboringEdges = [graph.Edges.First()];
            var lastEdge = neighboringEdges.First();
            foreach (var edge in graph.Edges.Except(neighboringEdges))
            {
                if (edge.FromNodeId == lastEdge.ToNodeId)
                {
                    neighboringEdges.Add(edge);
                    lastEdge = edge;
                }
                if (neighboringEdges.Count == count)
                {
                    break;
                }
            }
            return neighboringEdges;
        }
    }
}
