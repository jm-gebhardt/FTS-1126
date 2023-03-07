namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Models
{
    /// <summary>
    /// Graph made from Nodes and Edges
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class Graph
    {
        /// <summary>
        /// Nodes
        /// </summary>
        public Node[] Nodes { get; }

        /// <summary>
        /// Edges
        /// </summary>
        public Edge[] Edges { get; }

        /// <summary>
        /// Create new Graph
        /// </summary>
        /// <param name="nodes">Nodes</param>
        /// <param name="edges">Edges</param>
        public Graph(Node[] nodes, Edge[] edges)
        {
            Nodes = nodes;
            Edges = edges;
        }

        /// <summary>
        /// Get node by id
        /// </summary>
        /// <param name="nodeId">Id of node</param>
        /// <returns>Node or null if node was nto found</returns>
        public Node? GetNode(string nodeId)
        {
            foreach (var node in Nodes)
            {
                if (node.Id == nodeId)
                {
                    return node;
                }
            }

            return null;
        }
    }
}
