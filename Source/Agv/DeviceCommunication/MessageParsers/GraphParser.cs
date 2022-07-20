using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MessageParsers
{
    /// <summary>
    /// Parse <see cref="MqttGraph"/> message.
    /// </summary>
    public class GraphParser
    {
        private readonly MqttGraph _graph;

        private bool _checkedVelocity;
        private bool _considerVelocity;
        private List<AgvGraph.Node>? _nodes;
        private List<AgvGraph.Edge>? _edges;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graph"><see cref="MqttGraph"/> message.</param>
        public GraphParser(MqttGraph graph)
        {
            _graph = graph;
        }


        /// <summary>
        /// Calculate metric/costs for the <paramref name="edge"/>.
        /// </summary>
        /// <param name="edge">The edge for which the metric will be calculated.</param>
        /// <param name="nodes">List of nodes e.g. required for getting coordinates.</param>
        /// <param name="considerVelocity">When set to true the calculation expects to have a velocity
        /// and therefore metric is somehow time based.
        /// When set to false just the distance is used as metric.</param>
        /// <returns>Metric in a kind of distance/costs to travel this edge (unitless quantifier).</returns>
        /// <exception cref="InvalidOperationException">Nodes addressed by the edge cannot be found
        /// in the nodes listing or isn't unique..</exception>
        private static float CalculateMetric(Edge edge, Node[] nodes, bool considerVelocity)
        {
            var start = nodes.SingleOrDefault(x => x.NodeId == edge.StartId);
            var end = nodes.SingleOrDefault(x => x.NodeId == edge.EndId);

            if (start == null || end == null)
            {
                throw new InvalidOperationException("Node addressed by an edge is not part of the node listing.");
            }

            var vectorStart = new System.Numerics.Vector3(start.X, start.Y, start.Z ?? 0f);
            var vectorEnd = new System.Numerics.Vector3(end.X, end.Y, end.Z ?? 0f);

            if (considerVelocity)
            {
                // Calculate time based costs by distance divided by velocity.
                return (vectorStart - vectorEnd).Length() / edge.MaxVelocity;
            }
            else
            {
                // Calculate costs based on distance.
                return (vectorStart - vectorEnd).Length();
            }
        }

        /// <summary>
        /// Parse velocity from edges.
        /// </summary>
        /// <exception cref="InvalidOperationException">Edges are not valid (see <see cref="EdgesValid"/>).</exception>
        private void ParseVelocity()
        {
            if (!EdgesValid)
            {
                throw new InvalidOperationException($"Cannot handle {typeof(MqttGraph)} message, invalid number of edges.");
            }

            if (!_checkedVelocity)
            {
                _checkedVelocity = true;
                _considerVelocity = _graph.Edges!.All(x => x.MaxVelocity > 0.00001f); // Precision of single is about 6 to 9 digits.
            }

        }

        /// <summary>
        /// Parse edges.
        /// </summary>
        /// <exception cref="InvalidOperationException">Edges are not valid (see <see cref="EdgesValid"/>) or
        /// nodes are not valid (see <see cref="NodesValid"/>) or
        /// nodes do not match edges or
        /// node IDs are not unique.</exception>
        private void ParseEdges()
        {
            if (!NodesValid)
            {
                throw new InvalidOperationException($"Cannot handle {typeof(MqttGraph)} message, invalid number of nodes.");
            }

            ParseVelocity();
            if (_edges == null)
            {
                _edges = new List<AgvGraph.Edge>();

                foreach (var edge in _graph.Edges!)
                {
                    float metric = CalculateMetric(edge, _graph.Nodes!, _considerVelocity);

                    _edges.Add(new AgvGraph.Edge(edge.StartId.ToString(), edge.EndId.ToString())
                    {
                        Metric = metric
                    });

                    if (edge.Bidirectional)
                    {
                        // Build reversed edge by swapping From and To.
                        _edges.Add(new AgvGraph.Edge(edge.EndId.ToString(), edge.StartId.ToString())
                        {
                            Metric = metric
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Parse nodes.
        /// </summary>
        /// <exception cref="InvalidOperationException">Edges are not valid (see <see cref="EdgesValid"/>) or
        /// nodes are not valid (see <see cref="NodesValid"/>).</exception>
        private void ParseNodes()
        {
            if (!NodesValid)
            {
                throw new InvalidOperationException($"Cannot handle {typeof(MqttGraph)} message, invalid number of nodes.");
            }

            ParseEdges();
            if (_nodes == null)
            {
                _nodes = new List<AgvGraph.Node>();

                foreach (var node in _graph.Nodes!)
                {
                    _nodes.Add(new AgvGraph.Node(node.NodeId.ToString())
                    {
                        Enabled = true,

                        Type = AgvGraph.NodeType.WayPoint,
                        X = node.X,
                        Y = node.Y,
                        Z = node.Z ?? 0f
                    });
                    if (node.Attributes != null)
                    {
                        // Add stations beside end nodes as internal nodes.
                        foreach (var attr in node.Attributes)
                        {
                            Node.WellKnownAttributes unused;
                            if (Enum.TryParse(attr.Key, false, out unused))
                            {
                                _nodes.Add(new AgvGraph.Node(attr.Value)
                                {
                                    Enabled = true,

                                    Type = AgvGraph.NodeType.Station,
                                    X = node.X,
                                    Y = node.Y,
                                    Z = node.Z ?? 0f
                                });

                                // Add corresponding internal edges
                                _edges!.Add(new AgvGraph.Edge(attr.Value, node.NodeId.ToString())
                                {
                                    Metric = 0f
                                });
                                _edges!.Add(new AgvGraph.Edge(node.NodeId.ToString(), attr.Value)
                                {
                                    Metric = 0f
                                });
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns true when the serial number is present otherwise false.
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public bool SerialNumberValid => !string.IsNullOrEmpty(_graph.SerialNumber);

        /// <summary>
        /// Returns true when the nodes listing is present and contains at least one node.
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public bool EdgesValid => _graph.NumberOfEdges > 0 && _graph.Edges != null && _graph.NumberOfEdges == _graph.Edges.Length;

        /// <summary>
        /// Returns true when the nodes listing is present and contains at least one node.
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public bool NodesValid => _graph.NumberOfNodes > 0 && _graph.Nodes != null && _graph.NumberOfNodes == _graph.Nodes.Length;

        /// <summary>
        /// Returns true when velocity of all edges is present.
        /// </summary>
        /// <exception cref="InvalidOperationException">Edges are not valid (see <see cref="EdgesValid"/>).</exception>
        [JetBrains.Annotations.PublicAPI]
        public bool ConsiderVelocity
        {
            get
            {
                ParseVelocity();
                return _considerVelocity;
            }
        }

        /// <summary>
        /// Returns the serial number of the AGV.
        /// </summary>
        /// <exception cref="InvalidOperationException">Serial number is missing.</exception>
        [JetBrains.Annotations.PublicAPI]
        public string SerialNumber
        {
            get
            {
                if (!SerialNumberValid)
                {
                    throw new InvalidOperationException($"Cannot handle {typeof(MqttGraph)} message, serialNumber must not be null or empty.");
                }
                return _graph.SerialNumber;
            }
        }

        /// <summary>
        /// Returns the list of edges.
        /// </summary>
        /// <exception cref="InvalidOperationException">Edges are not valid (see <see cref="EdgesValid"/>) or
        /// nodes are not valid (see <see cref="NodesValid"/>) or
        /// nodes do not match edges or
        /// node IDs are not unique..</exception>
        [JetBrains.Annotations.PublicAPI]
        public List<AgvGraph.Edge> Edges
        {
            get
            {
                ParseEdges();
                return _edges!;
            }
        }

        /// <summary>
        /// Returns the list of nodes.
        /// </summary>
        /// <exception cref="InvalidOperationException">Nodes are not valid (see <see cref="NodesValid"/>) or
        /// edges are not valid (see <see cref="EdgesValid"/>).</exception>
        [JetBrains.Annotations.PublicAPI]
        public List<AgvGraph.Node> Nodes
        {
            get
            {
                ParseNodes();
                return _nodes!;
            }
        }

        /// <summary>
        /// Create AgvGraph
        /// </summary>
        public AgvGraph Graph =>
            new(SerialNumber.ToGuid(),
                Nodes.ToArray(),
                Edges.ToArray()
            );
    }
}
