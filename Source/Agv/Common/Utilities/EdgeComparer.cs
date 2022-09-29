using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.Common.Utilities
{
    /// <summary>
    /// Comparer to check for equal edges according to source and destination
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class EdgeIdComparer : IEqualityComparer<AgvGraph.Edge>
    {
        /// <inheritdoc />
        public bool Equals(AgvGraph.Edge? e1, AgvGraph.Edge? e2)
        {
            return e1 != null && e2 != null && e1.From == e2.From && e1.To == e2.To;
        }

        /// <inheritdoc />
        public int GetHashCode(AgvGraph.Edge e)
        {
            return HashCode.Combine(e.From, e.To);
        }
    }
}
