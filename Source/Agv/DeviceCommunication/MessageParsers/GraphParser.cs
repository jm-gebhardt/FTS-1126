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
        private float _overallDistance;
        private float _overallDuration;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graph"><see cref="MqttGraph"/> message.</param>
        public GraphParser(MqttGraph graph)
        {
            _graph = graph;
        }


        /// <summary>
        /// Calculate distance of the <paramref name="edge"/>.
        /// </summary>
        /// <param name="edge">The edge for which the metric will be calculated.</param>
        /// <param name="nodes">List of nodes e.g. required for getting coordinates.</param>
        /// <returns>Distance of the edge.</returns>
        /// <exception cref="InvalidOperationException">Nodes addressed by the edge cannot be found
        /// in the nodes listing or isn't unique..</exception>
        private static float CalculateDistance(Edge edge, Node[] nodes)
        {
            var start = nodes.SingleOrDefault(x => x.NodeId == edge.StartId);
            var end = nodes.SingleOrDefault(x => x.NodeId == edge.EndId);

            if (start == null || end == null)
            {
                throw new InvalidOperationException("Node addressed by an edge is not part of the node listing.");
            }

            var vectorStart = new System.Numerics.Vector3(start.X, start.Y, start.Z ?? 0f);
            var vectorEnd = new System.Numerics.Vector3(end.X, end.Y, end.Z ?? 0f);

            return (vectorStart - vectorEnd).Length();
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
        /// Helper to create new edge from AGV edge and calculate average graph attributes.
        /// </summary>
        private void BuildEdge(Edge edge, float metric, float distance, bool swap = false)
        {
            _edges!.Add(new AgvGraph.Edge(!swap ? edge.StartId.ToString() : edge.EndId.ToString(),
                                          !swap ? edge.EndId.ToString() : edge.StartId.ToString())
            {
                Metric = metric
            });
            if (_considerVelocity)
            {
                _overallDistance += distance;
                // No division by zero due to _considerVelocity.
                _overallDuration += distance / edge.MaxVelocity;
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
            if (!EdgesValid)
            {
                throw new InvalidOperationException($"Cannot handle {typeof(MqttGraph)} message, invalid number of edges.");
            }

            ParseVelocity();
            if (_edges == null)
            {
                _edges = new List<AgvGraph.Edge>();

                foreach (var edge in _graph.Edges!)
                {
                    float distance = CalculateDistance(edge, _graph.Nodes!);
                    float metric;
                    if (_considerVelocity)
                    {
                        // Calculate time based costs by distance divided by velocity
                        // (no division by zero due to _considerVelocity).
                        metric = distance / edge.MaxVelocity;
                    }
                    else
                    {
                        // Calculate costs based on distance.
                        metric = distance;
                    }

                    BuildEdge(edge, metric, distance);
                    if (edge.Bidirectional)
                    {
                        // Build reversed edge by swapping From and To.
                        BuildEdge(edge, metric, distance, true);
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
        public bool EdgesValid => _graph.Edges is { Length: > 0 };

        /// <summary>
        /// Returns true when the nodes listing is present and contains at least one node.
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public bool NodesValid => _graph.Nodes is { Length: > 0 } ;

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
        /// Returns the average max. velocity considering all edges.
        /// </summary>
        /// <exception cref="InvalidOperationException">Edges are not valid (see <see cref="EdgesValid"/>) or velocity information missing.</exception>
        [JetBrains.Annotations.PublicAPI]
        public float AverageVelocity
        {
            get
            {
                ParseEdges();
                if (!_considerVelocity)
                {
                    throw new InvalidOperationException("Velocity information missing.");
                }
                // No division by zero due to _considerVelocity.
                return _overallDistance / _overallDuration;
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
        /// Create AgvGraph.
        /// <para>Performs all required parsing and validation.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">Failed due to parsing or validation issue.</exception>
        public AgvGraph Graph =>
            new(SerialNumber.ToGuid(),
                Nodes.ToArray(),
                Edges.ToArray(),
                _graph.RelatedMapId.ToGuid(),
                _graph.RelatedMapId
            );
    }
}
