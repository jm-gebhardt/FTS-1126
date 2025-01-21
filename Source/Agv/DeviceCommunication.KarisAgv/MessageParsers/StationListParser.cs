using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.Common.Helpers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.MessageParsers;

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
    /// The name of the related map.
    /// </summary>
    public string? RelatedMapName { get; private set; }

    /// <summary>
    /// The Id of the related map.
    /// </summary>
    public Guid RelatedMapId { get; private set; }

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
    private float GetDistance(Station station, GraphNode node)
    {
        var vectorStation = new Vector3(station.Position.X, station.Position.Y, 0f);
        var vectorNode = new Vector3(node.X, node.Y, node.Z);

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
                _stationList.Stations.Length == 0)
            {
                _usability = AgvGraph.GraphUsability.InvalidStations;
            }

            if (_usability == AgvGraph.GraphUsability.Valid)
            {
                bool allAdded = true;
                HashSet<GraphNode> additionalNodes = new(new GraphNodeComparer());
                HashSet<GraphEdge> additionalEdges = new(new GraphEdgeComparer());

                foreach (var station in _stationList.Stations)
                {
                    // Search node from graph closest to this station.
                    GraphNode? nodeNearby = null;
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

                    if (nodeNearby == null)
                    {
                        throw new ArgumentException("No node nearby found");
                    }

                    // Add node and bidirectional edges.
                    var graphNode = new GraphNode(station.Identifier)
                    {
                        Type = GetNodeType(station.Function),
                        X = station.Position.X,
                        Y = station.Position.Y,
                        Z = 0f,
                        Theta = station.Position.Theta
                    };
                    bool added = additionalNodes.Add(graphNode);
                    if (added)
                    {
                        float metric = graphParser.ConsiderVelocity ? nearbyDistance / graphParser.AverageVelocity : nearbyDistance;
                        string graphNodeId = graphNode.Id;
                        string nodeNearbyId = nodeNearby.Id;
                        additionalEdges.Add(GraphEdge.CreateDirect(graphNodeId, nodeNearbyId, metric, GraphHelper.ToGuid(EdgeType.Direct, graphNodeId, metric, nodeNearbyId)));
                        additionalEdges.Add(GraphEdge.CreateDirect(nodeNearbyId, graphNodeId, metric, GraphHelper.ToGuid(EdgeType.Direct, nodeNearbyId, metric, graphNodeId)));
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

                _agvGraph = new AgvGraph(agvGraph.CorrelationId,
                    additionalNodes.ToArray(),
                    additionalEdges.ToArray(),
                    _usability);

                RelatedMapName = graphParser.RelatedMapName;
                RelatedMapId = graphParser.RelatedMapId;
            }
            else
            {
                _agvGraph = agvGraph;
                _agvGraph.Usability = _usability;
            }
        }
    }

    /// <summary>
    /// Converts station <see cref="Function"/> to <see cref="AgvGraph.NodeType"/>
    /// </summary>
    /// <param name="function">The function(s) of the station.</param>
    /// <returns>The Mqtt.StationList.Station.function converted/mapped to AgvGraph.NodeType</returns>
    private static object GetNodeType(object function)
    {
        bool isFunction = function is Function;
        if (isFunction)
        {
            return ParseNodeType((Function)function);
        }
        return ((List<Function>)function).Select(ParseNodeType).ToList();
    }

    /// <summary>
    /// Converts station <see cref="Function"/> to <see cref="AgvGraph.NodeType"/>
    /// </summary>
    /// <param name="function">The function of the station.</param>
    /// <returns>The Mqtt.StationList.Station.function converted/mapped to AgvGraph.NodeType</returns>
    private static AgvGraph.NodeType ParseNodeType(Function function)
    {
        return function switch
        {
            Function.Both => AgvGraph.NodeType.PickDrop,
            Function.Charge => AgvGraph.NodeType.Charge,
            Function.Drop => AgvGraph.NodeType.Drop,
            Function.Park => AgvGraph.NodeType.Park,
            Function.Pick => AgvGraph.NodeType.Pick,
            Function.Maintenance => AgvGraph.NodeType.Maintenance,
            _ => AgvGraph.NodeType.PickDrop
        };
    }

    /// <summary>
    /// Returns the serial number of the AGV.
    /// </summary>
    /// <exception cref="InvalidOperationException">Graph is invalid.</exception>
    [PublicAPI]
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