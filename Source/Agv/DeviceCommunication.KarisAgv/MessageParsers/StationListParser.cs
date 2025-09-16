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

    private bool _isParsed;

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
    public void Parse()
    {
        _isParsed = false;
        var graphParser = new GraphParser(_graph);
        graphParser.Parse();
        _agvGraph = graphParser.Graph;
        _usability = _agvGraph.Usability;
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
                foreach (var node in _agvGraph.Nodes)
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
                var graphNode = _agvGraph.Nodes.SingleOrDefault(n => n.Name == station.Identifier);
                bool added;
                if (graphNode == null)
                {
                    var timeStamp = _stationList.ModificationDate.UtcDateTime;
                    graphNode = new GraphNode(station.Identifier.ToGuid(), station.Identifier)
                    {
                        X = station.Position.X,
                        Y = station.Position.Y,
                        Z = 0f,
                        TimeStamp = timeStamp
                    };
                    graphNode.UpdateOrientation(station.Position.Theta);
                    graphNode.UpdateType(GetNodeType(station.Function));
                    added = additionalNodes.Add(graphNode);
                    if (added)
                    {
                        float metric = graphParser.IsVelocityDataValid ? nearbyDistance / graphParser.AverageVelocity : nearbyDistance;
                        string graphNodeName = graphNode.Name;
                        string nodeNearbyName = nodeNearby.Name;
                        var graphNodeId = graphNode.Id;
                        var nodeNearbyId = nodeNearby.Id;
                        additionalEdges.Add(GraphEdge.CreateDirect(graphNodeId, nodeNearbyId, metric, GraphHelper.ToGuid(EdgeType.Direct, graphNodeName, metric, nodeNearbyName), timeStamp));
                        additionalEdges.Add(GraphEdge.CreateDirect(nodeNearbyId, graphNodeId, metric, GraphHelper.ToGuid(EdgeType.Direct, nodeNearbyName, metric, graphNodeName), timeStamp));
                    }
                }
                else
                {
                    graphNode.UpdateOrientation(station.Position.Theta);
                    graphNode.UpdateType(GetNodeType(station.Function));
                    added = true;
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
                allAdded &= additionalNodes.AddRange(_agvGraph.Nodes);
                if (!allAdded)
                {
                    // Ambiguous station name compared to node names
                    _usability = AgvGraph.GraphUsability.AmbiguousStationNames;
                }
            }

            if (allAdded)
            {
                allAdded &= additionalEdges.AddRange(_agvGraph.Edges);
                if (!allAdded)
                {
                    // Edges not unique
                    _usability = AgvGraph.GraphUsability.StationEdgeMismatch;
                }
            }

            _agvGraph = new AgvGraph(_agvGraph.CorrelationId,
                additionalNodes.ToArray(),
                additionalEdges.ToArray(),
                _usability);

            RelatedMapName = graphParser.RelatedMapName;
            RelatedMapId = graphParser.RelatedMapId;
        }
        else
        {
            _agvGraph.Usability = _usability;
        }

        _isParsed = true;
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
            return ParseNodeType((Function) function);
        }
        return ((List<Function>) function).Select(ParseNodeType).ToList();
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

    private void VerifyGraphParsed()
    {
        if (!_isParsed)
        {
            throw new InvalidOperationException("Graph is not parsed yet. Call Parse() first.");
        }
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
            VerifyGraphParsed();
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
            VerifyGraphParsed();
            return _agvGraph!;
        }
    }
}