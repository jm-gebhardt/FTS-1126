using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MessageParsers
{
    /// <summary>
    /// Parse <see cref="MqttStationList"/>.
    /// </summary>
    public class StationListParser
    {
        private readonly MqttStationList _stationList;
        private readonly MqttGraph _graph;

        private string? _serialNumber;

        /// <summary>Indicator for graph usability (valid or not).</summary>
        private AgvGraph.GraphUsability _usability = AgvGraph.GraphUsability.Valid;
        private AgvGraph? _agvGraph;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="stationList"></param>
        public StationListParser(MqttStationList stationList, MqttGraph graph)
        {
            _stationList = stationList;
            _graph = graph;
        }


        /// <summary>
        /// Calculate distance between <paramref name="station"/> and <paramref name="node"/>.
        /// </summary>
        /// <param name="station"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private float GetDistance(Station station, Common.Messages.AgvManagement.AgvGraph.Node node)
        {
            var vectorStation = new System.Numerics.Vector3(station.Position.X, station.Position.Y, 0f);
            var vectorNode = new System.Numerics.Vector3(node.X, node.Y, node.Z);

            return (vectorStation - vectorNode).Length();
        }

        /// <summary>
        /// Parse graph and station list and create <see cref="AgvGraph"/> (to be stored in <see cref="_agvGraph"/>.
        /// <para>Performs all required parsing and validation.</para>
        /// </summary><remarks>
        /// This method is called by each public member.
        /// For diagnostic purpose the graph is build even when the graph is invalid.
        /// </remarks>
        private void Parse()
        {
            if (_agvGraph == null)
            {
                var graphParser = new GraphParser(_graph);
                var agvGraph = graphParser.Graph;
                _usability = agvGraph.Usability;
                _serialNumber = _stationList.SerialNumber;

                if (_usability == AgvGraph.GraphUsability.Valid &&
                    graphParser.SerialNumber != _serialNumber)
                {
                    _usability = AgvGraph.GraphUsability.SerialNumberMismatch;
                }

                if (_usability == AgvGraph.GraphUsability.Valid &&
                    _stationList.Stations.Length == 0 )
                {
                    _usability = AgvGraph.GraphUsability.InvalidStations;
                }

                if (_usability == AgvGraph.GraphUsability.Valid)
                {
                    bool allAdded = true;
                    HashSet<Common.Messages.AgvManagement.AgvGraph.Node> additionalNodes = new(new Common.Utilities.NodeIdComparer());
                    HashSet<Common.Messages.AgvManagement.AgvGraph.Edge> additionalEdges = new(new Common.Utilities.EdgeIdComparer());

                    foreach (var station in _stationList.Stations)
                    {
                        // Search node from graph closest to this station.
                        Common.Messages.AgvManagement.AgvGraph.Node? nodeNearby = null;
                        float nearbyDistance = 0f;
                        foreach (var node in agvGraph.Nodes)
                        {
                            float distance = GetDistance(station, node);
                            if (nodeNearby == null ||
                                distance < nearbyDistance)
                            {
                                nodeNearby = node;
                                nearbyDistance = distance;
                            }
                        }

                        // Add node and bidirectional edges.
                        bool added = additionalNodes.Add(new(station.Identifier)
                        {
                            Enabled = true,
                            Type = AgvGraph.NodeType.Station,
                            X = station.Position.X,
                            Y = station.Position.Y,
                            Z = 0f
                        });
                        if (added)
                        {
                            float metric = graphParser.ConsiderVelocity ? nearbyDistance / graphParser.AverageVelocity : nearbyDistance;
                            additionalEdges.Add(new(station.Identifier, nodeNearby!.Id)
                            {
                                Metric = metric
                            });
                            additionalEdges.Add(new(nodeNearby.Id, station.Identifier)
                            {
                                Metric = metric
                            });
                        }
                        allAdded &= added;
                    }
                    if (!allAdded)
                    {
                        // Station names not unique
                        _usability = AgvGraph.GraphUsability.InvalidStations;
                    }

                    if (allAdded)
                    {
                        allAdded &= additionalNodes.AddRange(agvGraph.Nodes);
                        if (!allAdded)
                        {
                            // Ambiguous station name compared to node names
                            _usability = AgvGraph.GraphUsability.AmbiguousStationNames;
                        }
                    }

                    if (allAdded)
                    {
                        allAdded &= additionalEdges.AddRange(agvGraph.Edges);
                        if (!allAdded)
                        {
                            // Edges not unique
                            _usability = AgvGraph.GraphUsability.StationEdgeMismatch;
                        }
                    }

                    _agvGraph = new AgvGraph(   agvGraph.CorrelationId,
                                                additionalNodes.ToArray(),
                                                additionalEdges.ToArray(),
                                                agvGraph.RelatedMapId,
                                                agvGraph.RelatedMapName,
                                                _usability);
                }
                else
                {
                    _agvGraph = agvGraph;
                    _agvGraph.Usability = _usability;
                }
            }
        }


        /// <summary>
        /// Returns the serial number of the AGV.
        /// </summary>
        /// <exception cref="InvalidOperationException">Graph is invalid.</exception>
        [JetBrains.Annotations.PublicAPI]
        public string SerialNumber
        {
            get
            {
                if (_agvGraph == null)
                {
                    Parse();
                }
                if (_agvGraph!.Usability != AgvGraph.GraphUsability.Valid)
                {
                    throw new InvalidOperationException("Graph is invalid.");
                }

                return _serialNumber!;
            }
        }

        /// <summary>
        /// Returns <see cref="AgvGraph"/> merged from graph and station list.
        /// <para>Performs all required parsing and validation once if required.</para>
        /// </summary>
        public AgvGraph Graph
        {
            get
            {
                if (_agvGraph == null)
                {
                    Parse();
                }

                return _agvGraph!;
            }
        }
    }
}
