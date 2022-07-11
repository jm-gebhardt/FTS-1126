using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;


namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// A* algorithm.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AStar : IGraphAlgorithm
    {
        /// <summary>
        /// Node extended with properties required for A* calculation.
        /// </summary>
        /// <remarks>
        /// All nodes (x) get a metric (costs) f(x)
        /// that is the best path from start to this node g(x)
        /// plus an estimation from this node to the destination h(x).
        /// <para>
        /// f(x) = g(x) + h(x)
        /// </para><para>
        /// <see cref="ANode.F"/> = <see cref="ANode.G"/> + <see cref="ANode.H"/>
        /// </para></remarks>
        private class ANode
        {
            /// <summary>
            /// Node representation.
            /// </summary>
            public AgvGraph.Node Node { get; }

            /// <summary>
            /// Reset members used for path calculation.
            /// </summary>
            public void Reset()
            {
                Predecessor = "";
                G = 0f;
                H = 0f;
            }

            /// <summary>
            /// Predecessor node ID of the calculated path.
            /// </summary>
            public string Predecessor { get; set; } = "";

            /// <summary>
            /// Distance (metric/costs) from start to this node.
            /// </summary>
            public float G { get; set; }

            /// <summary>
            /// Remaining (estimated) distance (metric/costs) to the destination.
            /// </summary>
            public float H { get; set; }

            /// <summary>
            /// (Estimation of) Costs(metric) from start to destination.
            /// </summary>
            public float F => G + H;

            /// <summary>
            /// Clone <see cref="Node"/>.
            /// </summary>
            /// <param name="node"></param>
            public ANode(AgvGraph.Node node)
            {
                Node = node;
            }

            /// <summary>
            /// Simple string representation.
            /// </summary>
            /// <returns></returns>
            public override string ToString() => $"{Node.Id} [{Predecessor}, F({F}) = G({G}) + H({H})]";
        }

        /// <summary>List of edges coming from the AGV graph.</summary>
        private AgvGraph.Edge[] _edges;

        /// <summary>List of nodes build from the AGV graph.</summary>
        private Dictionary<string, ANode> _nodes;

        /// <summary>List of nodes currently processing.</summary>
        private Dictionary<string, ANode> _openList; // Should be a PriorityQueue<node, float> but we need to update the sorting attribute.
        
        /// <summary>List of nodes already checked.</summary>
        private SortedSet<string> _closedList;


        /// <summary>
        /// A* implementation.
        /// </summary>
        /// <param name="agvGraph"></param>
        public AStar(AgvGraph agvGraph)
        {
            _edges = agvGraph.Edges;

            _nodes = new Dictionary<string, ANode>();
            foreach(var node in agvGraph.Nodes)
            {
                _nodes.Add(node.Id, new ANode(node));
            }

            _openList = new Dictionary<string, ANode>();
            _closedList = new SortedSet<string>();
        }


        /// <summary>
        /// Calculate distance between two nodes.
        /// </summary>
        /// <param name="first">One ot the nodes.</param>
        /// <param name="second">The other node.</param>
        /// <returns>Returns the euclidean distance.</returns>
        private float EuclideanDistance(ANode first, ANode second)
        {
            var vectorStart = new System.Numerics.Vector3(first.Node.X, first.Node.Y, first.Node.Z);
            var vectorEnd = new System.Numerics.Vector3(second.Node.X, second.Node.Y, second.Node.Z);

            return (vectorStart - vectorEnd).Length();
        }

        /// <summary>
        /// Helper to find and remove node from <see cref="_openList"/> with least costs.
        /// <para>
        /// We do not use LINQ because iteration is faster and less garbage.
        /// </para></summary>
        /// <returns>Returns the removed node with the minimal cost value or null when <see cref="_openList"/> is empty.</returns>
        private ANode? GetAndRemoveMin()
        {
            ANode? min = null;
            foreach(KeyValuePair<string, ANode> kvp in _openList)
            {
                if (min == null || kvp.Value.F < min.F)
                {
                    min = kvp.Value;
                }
            }

            if (min != null)
            {
                _openList.Remove(min.Node.Id);
            }
            return min;
        }

        /// <summary>
        /// Main function of the A* algorithm.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="destination"></param>
        /// <exception cref="KeyNotFoundException">Node not found in node collection.</exception>
        private void ExpandNode(ANode current, ANode destination)
        {
            foreach (var edge in _edges.Where(x => x.From == current.Node.Id))
            {
                if (_closedList.Any(x => x == edge.To))
                {
                    continue;
                }

                float g = current.G + edge.Metric;
                var successor = _nodes[edge.To];

                if (_openList.ContainsKey(successor.Node.Id) && g >= successor.G)
                {
                    continue;
                }

                successor.Predecessor = current.Node.Id;
                successor.G = g;
                if (successor.H == 0f && successor != destination)
                {
                    // Estimate distance to final destination.
                    successor.H = EuclideanDistance(successor, destination);
                }

                if (!_openList.ContainsKey(successor.Node.Id))
                {
                    _openList.Add(successor.Node.Id, successor);
                }
            }
        }


        /// <summary>
        /// Calculate costs of best path from source to destination.
        /// </summary>
        /// <param name="source">Node ID of the source.</param>
        /// <param name="destination">Node ID of the destination.</param>
        /// <returns>Returns the costs as a comparable value or null when no path found.</returns>
        /// <exception cref="KeyNotFoundException">Source or destination node not found.</exception>
        public float? Costs(string source, string destination)
        {
            _openList.Clear();
            _closedList.Clear();
            _nodes.Values.ToList().ForEach(x => x.Reset());

            var start = _nodes[source];
            var end = _nodes[destination];

            // Estimate distance to final destination.
            start.H = EuclideanDistance(start, end);

            ANode? current;
            _openList.Add(start.Node.Id, start);

            do
            {
                current = GetAndRemoveMin();
                if (current == end)
                {
                    // Found
                    break;
                }

                _closedList.Add(current!.Node.Id);

                ExpandNode(current, end);
            } while (_openList.Count != 0);

            if (end.Predecessor != "")
            {
                #if DEBUG
                current = end;
                // Write path found.
                System.Diagnostics.Debug.WriteLine(current.Node.Id);
                while (current.Node.Id != source)
                {
                    current = _nodes[current.Predecessor];
                    System.Diagnostics.Debug.WriteLine(current.Node.Id);
                }
                #endif

                return end.F;
            }
            else
            {
                // No path found.
                return null;
            }
        }

        /// <summary>
        /// Calculate route costs from <paramref name="source"/> to <paramref name="destination"/>.
        /// </summary>
        /// <param name="source">Node ID of the source.</param>
        /// <param name="destination">Node ID of the destination.</param>
        /// <returns>Returns the costs as a comparable value or null when no path found.</returns>
        /// <exception cref="InvalidOperationException">Source or destination node not found.</exception>
        public IGraphAlgorithm.Route? CalculateRoute(string source, string destination)
        {
            float? costs = Costs(source, destination);
            if (costs != null)
            {
                return new IGraphAlgorithm.Route()
                {
                    Costs = costs.Value
                };
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Calculate route costs from <paramref name="currentNode"/> to <paramref name="destination"/> via <paramref name="source"/>.
        /// </summary>
        /// <param name="currentNode">Node ID of the current position.</param>
        /// <param name="source">Node ID of the source.</param>
        /// <param name="destination">Node ID of the destination.</param>
        /// <returns>Returns the costs as a comparable value.</returns>
        /// <exception cref="InvalidOperationException">Source or destination node not found.</exception>
        public IGraphAlgorithm.Route? CalculateRoute(string currentNode, string source, string destination)
        {
            float? costs = Costs(source, destination);
            if (costs != null)
            {
                float? costsToStart = Costs(currentNode, source);
                costs = costsToStart ?? costs + costsToStart;
            }
            if (costs != null)
            {
                return new IGraphAlgorithm.Route()
                {
                    Costs = costs.Value
                };
            }
            else
            {
                return null;
            }
        }
    }
}
