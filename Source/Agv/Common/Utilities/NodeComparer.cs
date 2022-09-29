using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.Common.Utilities
{
    /// <summary>
    /// Comparer to check for equal node ID.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class NodeIdComparer : IEqualityComparer<AgvGraph.Node>
    {
        /// <inheritdoc />
        public bool Equals(AgvGraph.Node? n1, AgvGraph.Node? n2)
        {
            return n1 != null && n2 != null && n1.Id == n2.Id;
        }

        /// <inheritdoc />
        public int GetHashCode(AgvGraph.Node n)
        {
            return n.Id.GetHashCode();
        }
    }
}
