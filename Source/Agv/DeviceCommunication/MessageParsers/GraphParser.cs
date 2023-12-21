using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Exceptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Helpers;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.MessageParsers;

/// <summary>
/// Parse <see cref="MqttGraph"/> message.
/// </summary>
/// <remarks>
/// For diagnostic purpose the graph is built even when the graph is invalid.
/// </remarks>
public class GraphParser
{
    private readonly MqttGraph _graph;

    private string? _serialNumber;
    private readonly HashSet<GraphNode> _nodes;
    private readonly HashSet<GraphEdge> _edges;

    private bool _checkedVelocity;
    private bool _considerVelocity;
    private float _overallDistance;
    private float _overallDuration;

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
    /// <param name="graph"><see cref="MqttGraph"/> message.</param>
    public GraphParser(MqttGraph graph)
    {
        _graph = graph;
        _nodes = new HashSet<GraphNode>(new GraphNodeComparer());
        _edges = new HashSet<GraphEdge>(new GraphEdgeComparer());
    }

    /// <summary>
    /// Calculate distance of the <paramref name="mqttEdge"/>.
    /// </summary>
    /// <param name="mqttEdge">The edge for which the metric will be calculated.</param>
    /// <param name="nodes">List of nodes e.g. required for getting coordinates.</param>
    /// <returns>Distance of the edge.</returns>
    /// <exception cref="InvalidOperationException">Nodes addressed by the edge cannot be found
    /// in the nodes listing or isn't unique.</exception>
    private static float CalculateDistance(MqttEdge mqttEdge, MqttNode[] nodes)
    {
        var start = nodes.SingleOrDefault(x => x.NodeId == mqttEdge.StartId);
        var end = nodes.SingleOrDefault(x => x.NodeId == mqttEdge.EndId);

        if (start == null || end == null)
        {
            throw new InvalidOperationException("Node addressed by an edge is not part of the node listing.");
        }

        var vectorStart = new Vector3(start.X, start.Y, start.Z ?? 0f);
        var vectorEnd = new Vector3(end.X, end.Y, end.Z ?? 0f);

        return (vectorStart - vectorEnd).Length();
    }

    /// <summary>
    /// Parse velocity from edges.
    /// </summary>
    private void ParseVelocity()
    {
        if (!_checkedVelocity)
        {
            _checkedVelocity = true;
            _considerVelocity = _graph.Edges!.All(x => x.MaxVelocity > 0.00001f); // Precision of single is about 6 to 9 digits.
        }
    }

    /// <summary>
    /// Helper to create new edge from AGV edge and calculate average graph attributes.
    /// </summary>
    /// <returns>Returns true when add has been added</returns>
    private bool BuildEdge(MqttEdge mqttEdge, float metric, float distance, bool swap = false)
    {
        var (fromNode, toNode) = GetFromToNodes(mqttEdge);
        string fromNodeId = swap ? fromNode.Id : toNode.Id;
        string toNodeId = swap ? toNode.Id : fromNode.Id;
        var id = GraphHelper.ToGuid(fromNodeId, metric, toNodeId);
        var edge = GetGraphEdgeForCurveType(mqttEdge, metric, swap, fromNodeId, toNodeId, id);
        bool added = _edges.Add(edge);
        if (added && _considerVelocity)
        {
            _overallDistance += distance;
            // No division by zero due to _considerVelocity.
            _overallDuration += distance / mqttEdge.MaxVelocity;
        }

        return added;
    }

    private static GraphEdge GetGraphEdgeForCurveType(MqttEdge mqttEdge, float metric, bool swap, string fromNodeId, string toNodeId, Guid id)
    {
        ICollection<MqttControlPoint>? controlPoints = swap ? mqttEdge.ControlPoints?.Reverse().ToList() : mqttEdge.ControlPoints;
        bool parsed = Enum.TryParse<EdgeType>(mqttEdge.EdgeType, true, out var edgeType);
        if (!parsed)
        {
            edgeType = EdgeType.Direct;
        }
        switch (edgeType)
        {
            case EdgeType.Direct:
                return GraphEdge.CreateDirect(fromNodeId, toNodeId, metric, id);

            case EdgeType.Bezier1:
                if (controlPoints?.Count != 1)
                {
                    throw new MqttGraphParsingException($"Wrong number of control points for {nameof(EdgeType.Bezier1)} curve");
                }
                return GraphEdge.CreateBezier1(fromNodeId, toNodeId, metric, controlPoints.First().X, controlPoints.First().Y, id);

            case EdgeType.Bezier2:
                if (controlPoints?.Count != 2)
                {
                    throw new MqttGraphParsingException($"Wrong number of control points for {nameof(EdgeType.Bezier2)} curve");
                }
                return GraphEdge.CreateBezier2(fromNodeId, toNodeId, metric, controlPoints.First().X, controlPoints.First().Y, controlPoints.Last().X, controlPoints.Last().Y, id);

            case EdgeType.Polypath:
                if (controlPoints == null || controlPoints.Count < 1)
                {
                    throw new MqttGraphParsingException($"Wrong number of control points for {nameof(EdgeType.Polypath)} curve");
                }
                return GraphEdge.CreatePolypath(fromNodeId, toNodeId, metric, controlPoints.Select(c => new EdgeCoefficient(c.X, c.Y)).ToList(), id);

            case EdgeType.Slanted:
                return GraphEdge.CreateSlanted(fromNodeId, toNodeId, metric, mqttEdge.ExitAngle, mqttEdge.ExitDistance, id);

            default:
                return GraphEdge.CreateDirect(fromNodeId, toNodeId, metric, id);
        }
    }

    private (GraphNode fromNode, GraphNode toNode) GetFromToNodes(MqttEdge mqttEdge)
    {
        string fromNodeId = mqttEdge.StartId.ToString();
        string toNodeId = mqttEdge.EndId.ToString();
        var fromNode = _nodes.Single(n => n.Id == fromNodeId);
        var toNode = _nodes.Single(n => n.Id == toNodeId);
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

            allAdded &= BuildEdge(edge, metric, distance);
            if (edge.Bidirectional)
            {
                // Build reversed edge by swapping From and To.
                allAdded &= BuildEdge(edge, metric, distance, true);
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

        foreach (var node in _graph.Nodes!)
        {
            allAdded &= _nodes.Add(new GraphNode(node.NodeId.ToString())
            {
                Type = AgvGraph.NodeType.WayPoint,
                X = node.X,
                Y = node.Y,
                Z = node.Z ?? 0f
            });
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
    private void Parse()
    {
        if (_agvGraph == null)
        {
            if (!string.IsNullOrEmpty(_graph.SerialNumber))
            {
                _serialNumber = _graph.SerialNumber;
            }
            else
            {
                _usability = AgvGraph.GraphUsability.SerialNumberMissing;
            }

            if (_usability == AgvGraph.GraphUsability.Valid &&
                (_graph.Nodes!.Length == 0 ||
                 ParseNodes() == false))
            {
                _usability = AgvGraph.GraphUsability.InvalidNodes;
            }

            if (_usability == AgvGraph.GraphUsability.Valid &&
                (_graph.Edges!.Length == 0 ||
                 ParseEdges() == false))
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
            if (_agvGraph == null)
            {
                Parse();
            }
            if (_usability != AgvGraph.GraphUsability.Valid)
            {
                throw new InvalidOperationException("Graph is invalid.");
            }

            return _serialNumber!;
        }
    }

    /// <summary>
    /// Returns true when velocity of all edges is present.
    /// </summary>
    /// <exception cref="InvalidOperationException">Graph is invalid.</exception>
    [PublicAPI]
    public bool ConsiderVelocity
    {
        get
        {
            if (_agvGraph == null)
            {
                Parse();
            }
            if (_usability != AgvGraph.GraphUsability.Valid)
            {
                throw new InvalidOperationException("Graph is invalid.");
            }

            return _considerVelocity;
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
            if (_agvGraph == null)
            {
                Parse();
            }
            if (_usability != AgvGraph.GraphUsability.Valid)
            {
                throw new InvalidOperationException("Graph is invalid.");
            }
            if (!_considerVelocity)
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
            if (_agvGraph == null)
            {
                Parse();
            }

            return _agvGraph!;
        }
    }
}