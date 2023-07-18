using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.Common.Utilities
{
    /// <summary>
    /// Comparer to check for equal node ID.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class GraphNodeComparer : IEqualityComparer<GraphNode>
    {
        /// <inheritdoc />
        public bool Equals(GraphNode? n1, GraphNode? n2)
        {
            return n1 != null && n2 != null && n1.Id == n2.Id;
        }

        /// <inheritdoc />
        public int GetHashCode(GraphNode n)
        {
            return n.Id.GetHashCode();
        }
    }
}