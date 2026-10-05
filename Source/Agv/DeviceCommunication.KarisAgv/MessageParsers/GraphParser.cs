using Gebhardt.Acc.Common.Domain.Models.Errors;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Common.Helpers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Exceptions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Extensions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.MessageParsers;

/// <summary>
/// Parse <see cref="MqttGraph"/> message.
/// </summary>
/// <remarks>
/// For diagnostic purpose the graph is built even when the graph is invalid.
/// </remarks>
public class GraphParser
{
    private readonly MqttGraph _graph;
    private readonly HashSet<GraphNode> _nodes;
    private readonly HashSet<GraphEdge> _edges;

    private DateTime TimeStamp => _graph.ModificationDate.UtcDateTime;

    private bool _isParsed;

    private string? _serialNumber;
    private bool _checkedVelocity;
    private bool _isVelocityDataValid;
    private float _overallDistance;
    private float _overallDuration;

    private const int BezierResolution = 100;

    /// <summary>Indicator for graph usability (valid or not).</summary>
    private AgvGraph.GraphUsability _usability = AgvGraph.GraphUsability.Valid;

    private AgvGraph? _agvGraph;

    /// <summary>
    /// The name of the related map.
    /// </summary>
    public string? RelatedMapName { get; private set; }

    /// <summary>
    /// The ID of the related map.
    /// </summary>
    public Guid RelatedMapId { get; private set; }

    /// <summary>
    /// Dictionary of MqttEdges by their ID.
    /// </summary>
    public Dictionary<Guid, MqttEdge> EdgesById { get; } = new();

    /// <summary>
    /// Dictionary of MqttNodes by their ID.
    /// </summary>
    public Dictionary<string, MqttNode> NodesById { get; } = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="graph"><see cref="MqttGraph"/> message.</param>
    public GraphParser(MqttGraph graph)
    {
        _graph = graph;
        _nodes = new HashSet<GraphNode>(new GraphNodeComparer());
        _edges = new HashSet<GraphEdge>(new GraphEdgeComparer());
    }

    /// <summary>
    /// Calculate length of the <paramref name="mqttEdge"/>.
    /// </summary>
    /// <param name="mqttEdge">The edge for which the metric will be calculated.</param>
    /// <param name="nodes">List of nodes e.g. required for getting coordinates.</param>
    /// <returns>Distance of the edge.</returns>
    /// <exception cref="MqttGraphParsingException">Nodes addressed by the edge cannot be found
    /// in the nodes listing or isn't unique.</exception>
    /// <exception cref="ArgumentOutOfRangeException"> Unknown edge type.</exception>
    private static double CalculateEdgeLength(MqttEdge mqttEdge, MqttNode[] nodes)
    {
        var start = nodes.SingleOrDefault(x => x.NodeId == mqttEdge.StartId);
        var end = nodes.SingleOrDefault(x => x.NodeId == mqttEdge.EndId);

        bool parsed = Enum.TryParse<EdgeType>(mqttEdge.EdgeType, true, out var edgeType);
        if (!parsed)
        {
            edgeType = EdgeType.Direct;
        }

        if (start == null || end == null)
        {
            throw new MqttGraphParsingException($"Edge '{mqttEdge.EdgeId}': Node '" + (start == null ? mqttEdge.StartId : mqttEdge.EndId) + "' addressed by edge is not part of the node listing.");
        }

        double length = edgeType switch
        {
            EdgeType.Bezier2 or EdgeType.Bezier3 or EdgeType.Bezier5 => mqttEdge.ToBezier(start, end).GetLength(BezierResolution),
            EdgeType.Polypath => mqttEdge.ToPolypath(start, end).GetLength(),
            EdgeType.Slanted => mqttEdge.ToSlanted(start, end).GetLength(),
            EdgeType.Direct => mqttEdge.ToDirect(start, end).GetLength(),
            _ => throw new ArgumentOutOfRangeException($"Unhandled edge type: {edgeType}")
        };
        return length;
    }

    private void VerifyGraphParsed()
    {
        if (!_isParsed)
        {
            throw new InvalidOperationException("Graph is not parsed yet. Call Parse() first.");
        }
    }

    private void VerifyGraphValid()
    {
        VerifyGraphParsed();
        if (_usability != AgvGraph.GraphUsability.Valid)
        {
            throw new InvalidOperationException("Graph is invalid.");
        }
    }

    /// <summary>
    /// Parse velocity from edges.
    /// </summary>
    private void ParseVelocity()
    {
        if (_checkedVelocity)
        {
            return;
        }
        _checkedVelocity = true;
        _isVelocityDataValid = _graph.Edges!.All(x => x.MaxVelocity > Mathematics.FloatComparator); // Precision of single is about 6 to 9 digits.
    }

    /// <summary>
    /// Helper to create new edge from AGV edge and calculate graph attributes.
    /// </summary>
    /// <returns>Returns true when the edge has been added</returns>
    private bool BuildEdge(MqttEdge mqttEdge, float metric, double length, bool swap = false)
    {
        var (fromNode, toNode) = GetFromToNodes(mqttEdge);
        string fromNodeId = swap ? toNode.Name : fromNode.Name;
        string toNodeId = swap ? fromNode.Name : toNode.Name;

        var edge = GetGraphEdgeForCurveType(mqttEdge, metric, (float) length, swap, fromNodeId, toNodeId);
        bool allAdded = _edges.Add(edge);

        if (allAdded && _isVelocityDataValid)
        {
            _overallDistance += (float) length;
            // No division by zero due to _considerVelocity.
            _overallDuration += (float) length / mqttEdge.MaxVelocity;
        }
        else if (!allAdded)
        {
            throw new MqttGraphParsingException($"Edge '{mqttEdge.EdgeId}' is not unique (StartId: '{mqttEdge.StartId}', EndId: '{mqttEdge.EndId}', EdgeType: '{mqttEdge.EdgeType}')");
        }

        if (allAdded)
        {
            EdgesById.Add(edge.Id, mqttEdge);
            // this would throw as the edge is not unique. Usability will be set to invalid outside.
        }

        return allAdded;
    }

    private GraphEdge GetGraphEdgeForCurveType(MqttEdge mqttEdge, float metric, float length, bool swap, string fromNodeName, string toNodeName)
    {
        var controlPoints = swap ? mqttEdge.ControlPoints?.Reverse().ToList() : mqttEdge.ControlPoints;
        bool parsed = Enum.TryParse<EdgeType>(mqttEdge.EdgeType, true, out var edgeType);
        if (!parsed)
        {
            edgeType = EdgeType.Direct;
        }
        var fromNodeId = fromNodeName.ToGuid();
        var toNodeId = toNodeName.ToGuid();
        var id = GraphHelper.ToGuid(edgeType, fromNodeName, metric, toNodeName);
        switch (edgeType)
        {
            case EdgeType.Direct:
                return GraphEdge.CreateDirect(fromNodeId, toNodeId, length, id, TimeStamp);

            case EdgeType.Bezier2:

                if (controlPoints?.Count != 1)
                {
                    throw new MqttGraphParsingException($"Edge '{mqttEdge.EdgeId}': Wrong number of control points for {nameof(EdgeType.Bezier2)} curve");
                }
                return GraphEdge.CreateBezier2(fromNodeId, toNodeId, length, controlPoints.First().X, controlPoints.First().Y, id, TimeStamp);

            case EdgeType.Bezier3:
                if (controlPoints?.Count != 2)
                {
                    throw new MqttGraphParsingException($"Edge '{mqttEdge.EdgeId}': Wrong number of control points for {nameof(EdgeType.Bezier3)} curve");
                }
                return GraphEdge.CreateBezier3(fromNodeId, toNodeId, length, controlPoints.First().X, controlPoints.First().Y, controlPoints.Last().X, controlPoints.Last().Y, id, TimeStamp);

            case EdgeType.Bezier5:
                if (controlPoints?.Count != 4)
                {
                    throw new MqttGraphParsingException($"Edge '{mqttEdge.EdgeId}': Wrong number of control points for {nameof(EdgeType.Bezier5)} curve");
                }
                return GraphEdge.CreateBezier5(fromNodeId, toNodeId, length, controlPoints.Select(c => new EdgeCoefficient(c.X, c.Y)).ToArray(), id, TimeStamp);

            case EdgeType.Polypath:
                if (controlPoints == null || controlPoints.Count < 1)
                {
                    throw new MqttGraphParsingException($"Edge '{mqttEdge.EdgeId}': Wrong number of control points for {nameof(EdgeType.Polypath)} curve");
                }
                return GraphEdge.CreatePolypath(fromNodeId, toNodeId, length, controlPoints.Select(c => new EdgeCoefficient(c.X, c.Y)).ToList(), id, TimeStamp);

            case EdgeType.Slanted:
                return GraphEdge.CreateSlanted(fromNodeId, toNodeId, length, mqttEdge.ExitAngle, mqttEdge.ExitDistance, id, TimeStamp);

            default:
                return GraphEdge.CreateDirect(fromNodeId, toNodeId, length, id, TimeStamp);
        }
    }

    private (GraphNode fromNode, GraphNode toNode) GetFromToNodes(MqttEdge mqttEdge)
    {
        string fromNodeId = mqttEdge.StartId;
        string toNodeId = mqttEdge.EndId;
        var fromNode = _nodes.Single(n => n.Name == fromNodeId);
        var toNode = _nodes.Single(n => n.Name == toNodeId);
        return (fromNode, toNode);
    }

    /// <summary>
    /// Parse edges.
    /// </summary>
    /// <returns>Returns true when all edges parsed successfully and are unique.</returns>
    private bool ParseEdges()
    {
        bool allAdded = true;
        ParseVelocity();

        foreach (var edge in _graph.Edges!)
        {
            double length = CalculateEdgeLength(edge, _graph.Nodes!);
            float metric;
            if (_isVelocityDataValid)
            {
                // Calculate time based costs by distance divided by velocity
                // (no division by zero due to _considerVelocity).
                metric = (float) length / edge.MaxVelocity;
            }
            else
            {
                // Calculate costs based on distance.
                metric = (float) length;
            }
            bool edgeAdded = BuildEdge(edge, metric, length);
            allAdded &= edgeAdded;
 
            if (edge.Bidirectional)
            {
                // Build reversed edge by swapping From and To.
                allAdded &= BuildEdge(edge, metric, length, true);
            }
        }

        return allAdded;
    }

    /// <summary>
    /// Parse nodes.
    /// </summary>
    /// <returns>Returns true when all nodes parsed successfully and are unique.</returns>
    private bool ParseNodes()
    {
        bool allAdded = true;
        
        foreach (var mqttNode in _graph.Nodes!)
        {
            var newNode = new GraphNode(mqttNode.NodeId.ToGuid(), mqttNode.NodeId)
            {
                Type = [AgvGraph.NodeType.WayPoint],
                TimeStamp = TimeStamp,
                X = mqttNode.X,
                Y = mqttNode.Y,
                Z = mqttNode.Z ?? 0f
            };
            bool nodeAdded = _nodes.Add(newNode);
            allAdded &= nodeAdded;
            if (!nodeAdded)
            {
                throw new MqttGraphParsingException($"Node '{mqttNode.NodeId}' is not unique.)");
            }

            if (allAdded)
            {
                NodesById.Add(newNode.Name, mqttNode);
                // this would throw as the node is not unique. But with allAdded it's safe. Usability will be set to invalid outside.
            }
        }

        return allAdded;
    }

    /// <summary>
    /// Parse graph message and create <see cref="AgvGraph"/> (to be stored in <see cref="_agvGraph"/>.
    /// <para>Performs all required parsing and validation.</para>
    /// </summary><remarks>
    /// This method is called by each public member.
    /// For diagnostic purpose the graph is built even when the graph is invalid.
    /// </remarks>
    public Result Parse()
    {
        Error? resultError = null;
        _usability = AgvGraph.GraphUsability.Valid;
        _serialNumber = null;
        _checkedVelocity = false;
        _isVelocityDataValid = false;
        _overallDistance = 0f;
        _overallDuration = 0f;
        _isParsed = false;
        _nodes.Clear();
        _edges.Clear();
        EdgesById.Clear();
        NodesById.Clear();

        if (!string.IsNullOrEmpty(_graph.SerialNumber))
        {
            _serialNumber = _graph.SerialNumber;
        }
        else
        {
            _usability = AgvGraph.GraphUsability.SerialNumberMissing;
        }

        if (_usability == AgvGraph.GraphUsability.Valid &&
            (_graph.Nodes == null || _graph.Nodes.Length == 0 ||
             !ParseNodes()))
        {
            _usability = AgvGraph.GraphUsability.InvalidNodes;
        }

        if (_usability == AgvGraph.GraphUsability.Valid &&
            (_graph.Edges == null || _graph.Edges.Length == 0 ||
             !ParseEdges()))
        {
            _usability = AgvGraph.GraphUsability.InvalidEdges;
        }

        _agvGraph = new AgvGraph(_serialNumber?.ToGuid() ?? Guid.Empty,
            _nodes,
            _edges,
            _usability
        );

        RelatedMapName = _graph.RelatedMapId;
        RelatedMapId = _graph.RelatedMapId.ToGuid();
        _isParsed = true;

        return _usability == AgvGraph.GraphUsability.Valid ? Result.Success() : Result.Failure(resultError ?? new Error("Unknown error during graph parsing."));
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
            VerifyGraphValid();
            return _serialNumber!;
        }
    }

    /// <summary>
    /// Returns true when velocity of all edges is present.
    /// </summary>
    /// <exception cref="InvalidOperationException">Graph is invalid.</exception>
    [PublicAPI]
    public bool IsVelocityDataValid
    {
        get
        {
            VerifyGraphValid();
            return _isVelocityDataValid;
        }
    }

    /// <summary>
    /// Returns the average max. velocity considering all edges.
    /// </summary>
    /// <exception cref="InvalidOperationException">Graph is invalid or velocity information is missing and therefore the average speed is unknown.</exception>
    [PublicAPI]
    public float AverageVelocity
    {
        get
        {
            VerifyGraphValid();
            if (!_isVelocityDataValid)
            {
                throw new InvalidOperationException("Velocity information missing.");
            }
            // No division by zero due to _considerVelocity.
            return _overallDistance / _overallDuration;
        }
    }

    /// <summary>
    /// Returns <see cref="AgvGraph"/>.
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