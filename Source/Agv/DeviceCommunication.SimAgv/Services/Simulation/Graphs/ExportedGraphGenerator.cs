using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using System.Numerics;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.Common.Helpers;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Configuration;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

/// <summary>
/// Generate a graph based on the one exported from Karis
/// </summary>
public class ExportedGraphGenerator: GraphGeneratorBase
{
    private readonly IGraphReader _reader;

    private  List<GraphNode> _nodes = [];
    private  List<GraphEdge> _edges = [];
    private readonly Dictionary<string, string> _waypointNames = new();

    private readonly KarisExportConfiguration _configuration;

    private GraphNode[] WayPoints => _nodes.Where(n => n.Type == AgvGraph.NodeType.WayPoint).ToArray();
    
    private ExportedGraph _exportedGraph = null!;

    /// <inheritdoc cref="ExportedGraphGenerator"/>>
    public ExportedGraphGenerator(IMapGenerator mapGenerator, IGraphReader reader, KarisExportConfiguration configuration) : base([], [], mapGenerator)
    {
        _reader = reader;
        _configuration = configuration;
    }

    /// <inheritdoc />
    protected override Guid GraphId { get; } = "ExportedGraph".ToGuid();
    
    /// <inheritdoc />
    public override AgvGraph Generate(Guid correlationId)
    {
        _nodes = [];
        _edges = [];

        _exportedGraph = _reader.Read();
        
        GetNodes();
        GetEdges();
        ConnectStations();

        return new AgvGraph(correlationId, _nodes.ToArray(), _edges);
    }

    private void GetNodes()
    {
        foreach (var exportedNode in _exportedGraph.Nodes)
        {
            var node = new GraphNode(GetNodeName(exportedNode.Name), (float)exportedNode.X, -(float)exportedNode.Y, 0, GetNodeType(exportedNode.Name));
            _nodes.Add(node);
        }
    }

    private string GetNodeName(string exportedName)
    {
        if (!_configuration.RenameWaypoints.HasValue || !_configuration.RenameWaypoints!.Value)
        {
            return exportedName;
        }

        var nodeType = GetNodeType(exportedName);
        if (nodeType == AgvGraph.NodeType.WayPoint)
        {
            if (_waypointNames.TryGetValue(exportedName, out string? adjustedName))
            {
                return adjustedName;
            }
            adjustedName = $"WP-{WayPoints.Length.ToString().PadLeft(4, '0')}";
            _waypointNames[exportedName] = adjustedName;
            return adjustedName;
        }
        return exportedName;
    }

    private void GetEdges()
    {
        foreach (var exportedSegment in _exportedGraph.Segments)
        {
            float length = GetLength(exportedSegment.Points.Select(p => p.InvertY().AsVector2()).ToList());
            EdgeCoefficient[] edgeCoefficients = exportedSegment.Points.Skip(1).Take(4).Select(c => new EdgeCoefficient(c.X, -c.Y)).ToArray();
            string startNode = GetNodeName(exportedSegment.StartNode);
            string endNode = GetNodeName(exportedSegment.EndNode);
            var edge = GraphEdge.CreateBezier5(startNode, endNode, length,
                edgeCoefficients,
                GraphHelper.ToGuid(EdgeType.Bezier5, startNode, length, endNode));
            _edges.Add(edge);
        }
    }

    private void ConnectStations()
    {
        foreach (var station in _nodes.Where(n => n.Type != AgvGraph.NodeType.WayPoint))
        {
            (var nodeNearby, float distance) = GetNodeNearby(station);

            if (!_edges.Any(e => e.ToNodeId == nodeNearby.Id && e.FromNodeId == station.Id))
            {
                var edge = GraphEdge.CreateDirect(station.Id, nodeNearby.Id, distance, GraphHelper.ToGuid(EdgeType.Direct, station.Id, distance, nodeNearby.Id));
                _edges.Add(edge);
            }
            if (!_edges.Any(e => e.FromNodeId == nodeNearby.Id && e.ToNodeId == station.Id))
            {
                var reverse = GraphEdge.CreateDirect(nodeNearby.Id, station.Id, distance, GraphHelper.ToGuid(EdgeType.Direct, nodeNearby.Id, distance, station.Id));
                _edges.Add(reverse);
            }

            station.Theta = (float)Mathematics.OrientationToXAxis(nodeNearby.AsVector2(), station.AsVector2());
        }
    }

    // Search waypoint closest to the station.
    private (GraphNode, float) GetNodeNearby(GraphNode station)
    {
        GraphNode? nodeNearby = null;
        double nearbyDistance = 0f;
        foreach (var node in WayPoints)
        {
            double distance = Mathematics.EuclideanDistance(station, node);
            if (nodeNearby == null || distance < nearbyDistance)
            {
                nodeNearby = node;
                nearbyDistance = distance;
            }
        }

        if (nodeNearby == null)
        {
            throw new ArgumentException("No node nearby found");
        }
        return (nodeNearby, (float)nearbyDistance);
    }

    private AgvGraph.NodeType GetNodeType(string nodeName)
    {
        if (_configuration.ChargeRegex.IsMatch(nodeName))
        {
            return AgvGraph.NodeType.Charge;
        }

        if (_configuration.WayPointRegex.IsMatch(nodeName))
        {
            return AgvGraph.NodeType.WayPoint;
        }

        if (_configuration.ParkRegex.IsMatch(nodeName))
        {
            return AgvGraph.NodeType.Park;
        }

        if (_configuration.PickRegex.IsMatch(nodeName))
        {
            return AgvGraph.NodeType.Pick;
        }

        if (_configuration.DropRegex.IsMatch(nodeName))
        {
            return AgvGraph.NodeType.Drop;
        }

        return AgvGraph.NodeType.PickDrop;
    }

    private static float GetLength(List<Vector2> controlPoints)
    {
        var bezier = new Bezier(controlPoints);
        return (float)bezier.GetLength(100);
    }
}