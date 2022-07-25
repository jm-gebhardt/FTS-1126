namespace Gebhardt.StoreWare.Agv.Common.Definitions
{
    /// <summary>
    /// Interface for graph algorithm used to calculate route costs.
    /// </summary>
    public interface IGraphAlgorithm
    {
        /// <summary>
        /// Represents the route costs.
        /// </summary>
        public class Route : IComparable<Route>
        {
            /// <summary>
            /// Route Constructor
            /// </summary>
            /// <param name="costs"></param>
            /// <param name="nodes"></param>
            public Route(float costs, string[] nodes)
            {
                Costs = costs;
                Nodes = nodes;
            }

            /// <summary>
            /// A comparable value representing some kind of costs this route will take.
            /// </summary>
            [JetBrains.Annotations.PublicAPI]
            public float Costs { get; set; }
            
            /// <summary>
            /// List of node ids that define the route
            /// </summary>
            [JetBrains.Annotations.PublicAPI]
            public string[] Nodes { get; set; }

            /// <summary>
            /// Used for sorting, nulls last.
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public int CompareTo(Route? other)
            {
                if (other != null)
                {
                    if (Costs < other.Costs)
                    {
                        return -1;
                    }
                    if (Costs > other.Costs)
                    {
                        return 1;
                    }
                    return 0;
                }
                return 2;
            }
            
            /// <summary>
            /// Combine two routes
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            public static Route operator +(Route a, Route b)
            {
                string[] nodes = a.Nodes.LastOrDefault() == b.Nodes.FirstOrDefault() 
                    ? a.Nodes.Concat(b.Nodes.Skip(1)).ToArray() // Skip first item of b if first and last are equal
                    : a.Nodes.Concat(b.Nodes).ToArray();

                return new Route(a.Costs + b.Costs, nodes);
            }
        }
        /// <summary>
        /// Calculate route costs from <paramref name="source"/> to <paramref name="destination"/>.
        /// </summary>
        /// <param name="source">Source</param>
        /// <param name="destination">Destination</param>
        /// <returns>Returns the route costs of the best route. When no route exists null is returned.</returns>
        [JetBrains.Annotations.PublicAPI]
        public Route? CalculateRoute(string source, string destination);

        /// <summary>
        /// Calculate route costs from <paramref name="currentNode"/> to <paramref name="destination"/> via <paramref name="source"/>.
        /// </summary>
        /// <param name="currentNode">Current position.</param>
        /// <param name="source">Source</param>
        /// <param name="destination">Destination</param>
        /// <returns>Returns the route costs of the best route. When no route exists null is returned.</returns>
        [JetBrains.Annotations.PublicAPI]
        public Route? CalculateRoute(string currentNode, string source, string destination);
    }
}
