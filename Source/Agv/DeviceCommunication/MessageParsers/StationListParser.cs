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
        /// Parse graph.
        /// </summary>
        /// <exception cref="InvalidOperationException">Graph is not valid (see <see cref="GraphParser"/>).</exception>
        [JetBrains.Annotations.PublicAPI]
        private void ParseGraph()
        {
            if (_agvGraph == null)
            {
                var graphParser = new GraphParser(_graph);
                _agvGraph = graphParser.Graph;
            }
        }


        /// <summary>
        /// Returns true when the serial number is present otherwise false.
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public bool SerialNumberValid =>
            !string.IsNullOrEmpty(_stationList.SerialNumber) &&
            _stationList.SerialNumber == _graph.SerialNumber;

        /// <summary>
        /// Returns true when the nodes listing is present and contains at least one node.
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public bool StationListValid => _stationList.Stations.Length > 0;


        /// <summary>
        /// Create AgvGraph.
        /// <para>Performs all required parsing and validation.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">Failed due to parsing or validation issue.</exception>
        public AgvGraph Graph
        {
            get
            {
                if (_agvGraph == null)
                {
                    var graphParser = new GraphParser(_graph);
                    var agvGraph = graphParser.Graph;
                    if (!StationListValid)
                    {
                        throw new InvalidOperationException("Station list is missing or empty.");
                    }

                    List<Common.Messages.AgvManagement.AgvGraph.Node> additionalNodes = new();
                    List<Common.Messages.AgvManagement.AgvGraph.Edge> additionalEdges = new();

                    foreach (var station in _stationList.Stations)
                    {
                        // Search node closest to this station.
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
                        additionalNodes.Add(new(station.Identifier)
                        {
                            Enabled = true,
                            Type = AgvGraph.NodeType.Station,
                            X = station.Position.X,
                            Y = station.Position.Y,
                            Z = 0f
                        });
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

                    additionalNodes.AddRange(agvGraph.Nodes);
                    additionalEdges.AddRange(agvGraph.Edges);

                    _agvGraph = new AgvGraph(agvGraph.CorrelationId, additionalNodes.ToArray(), additionalEdges.ToArray(), agvGraph.RelatedMapId, agvGraph.RelatedMapName);
                }
                return _agvGraph;
            }
        }
    }
}
