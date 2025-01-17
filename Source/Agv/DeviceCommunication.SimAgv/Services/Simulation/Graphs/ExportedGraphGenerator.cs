using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using Gebhardt.Acc.DeviceCommunication.Common.Helpers;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Extensions;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

/// <summary>
/// Generate a graph based on the one exported from Karis
/// </summary>
public class ExportedGraphGenerator: GraphGeneratorBase
{
    private readonly IGraphReader _reader;

    private  List<GraphNode> _nodes = [];
    private  List<GraphEdge> _edges = [];

    private GraphNode[] WayPoints => _nodes.Where(n => !n.IsStation()).ToArray();
    
    private ExportedGraph _exportedGraph = null!;

    /// <inheritdoc cref="ExportedGraphGenerator"/>>
    public ExportedGraphGenerator(IMapGenerator mapGenerator, IGraphReader reader) : base([], [], mapGenerator)
    {
        _reader = reader;
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
            _nodes.Add(exportedNode.ToGraphNode());
        }
        foreach (var exportedStation in _exportedGraph.Stations)
        {
            _nodes.Add(exportedStation.ToGraphNode());
        }
    }

    private void GetEdges()
    {
        foreach (var exportedEdge in _exportedGraph.Edges)
        {
            _edges.AddRange(exportedEdge.ToGraphEdges(GetNode(exportedEdge.StartId), GetNode(exportedEdge.EndId)));
        }
    }

    private GraphNode GetNode(string nodeId)
    {
        return _nodes.Single(n => n.Id == nodeId);
    }

    private void ConnectStations()
    {
        foreach (var station in _nodes.Where(n => n.IsStation()))
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
}