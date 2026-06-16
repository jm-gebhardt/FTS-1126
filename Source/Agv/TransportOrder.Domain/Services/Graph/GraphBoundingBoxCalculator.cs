using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using CommonGraph = Gebhardt.Acc.Common.Domain.Models.Graphs.Graph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

/// <summary>
/// Computes oriented bounding box polygons for every node and edge in a graph for a given AGV type.
/// </summary>
[PublicAPI]
public class GraphBoundingBoxCalculator
{
    private readonly CommonGraph _graph;
    private readonly IReadOnlyList<IEdgePolygonProvider> _providers;
    private readonly Dictionary<Guid, GraphNode> _nodesById;
    private readonly Dictionary<Guid, double> _nodeOrientations;

    /// <inheritdoc cref="GraphBoundingBoxCalculator"/>
    public GraphBoundingBoxCalculator(CommonGraph graph, IEnumerable<IEdgePolygonProvider> providers)
    {
        _graph = graph;
        _providers = providers.ToList();
        _nodesById = graph.Nodes.ToDictionary(n => n.Id);
        _nodeOrientations = [];

        foreach (var edge in graph.Edges)
        {
            var from = _nodesById[edge.FromNodeId];
            var to = _nodesById[edge.ToNodeId];
            var provider = SelectProvider(edge.Variant);
            _nodeOrientations.TryAdd(edge.FromNodeId, provider.GetDirectionAtEndpoint(edge, from, to, EdgeEndpoint.From));
            _nodeOrientations.TryAdd(edge.ToNodeId, provider.GetDirectionAtEndpoint(edge, from, to, EdgeEndpoint.To));
        }
    }

    private IEdgePolygonProvider SelectProvider(EdgeType edgeType)
    {
        return _providers.First(p => p.Supports(edgeType));
    }

    /// <summary>
    /// Calculates bounding boxes for all nodes and edges in the graph.
    /// </summary>
    public IReadOnlyList<ResourceBoundingBox> CalculateAll(AgvData agvData, IReadOnlyList<IMarginStrategy> margins)
    {
        var results = new List<ResourceBoundingBox>(_graph.Nodes.Count + _graph.Edges.Count);
        foreach (var node in _graph.Nodes)
        {
            results.Add(CalculateNode(node, agvData, margins));
        }

        foreach (var edge in _graph.Edges)
        {
            results.Add(CalculateEdge(edge, agvData, margins));
        }

        return results;
    }

    /// <summary>
    /// Calculates the bounding box for a single node. The box is oriented along the node's first incident edge, isolated nodes fall back to Theta/>.
    /// </summary>
    public ResourceBoundingBox CalculateNode(GraphNode node, AgvData agvData, IReadOnlyList<IMarginStrategy> margins)
    {
        double hL = agvData.Length / 2;
        double hW = agvData.Width / 2;
        double theta = _nodeOrientations.TryGetValue(node.Id, out double edgeTheta) ? edgeTheta : node.Theta;
        double cosT = Math.Cos(theta);
        double sinT = Math.Sin(theta);

        // Local corners in CCW order: back-right, front-right, front-left, back-left.
        (double Lx, double Ly)[] local = [(-hL, -hW), (hL, -hW), (hL, hW), (-hL, hW)];
        var rawPolygon = new Polygon(local.Select(p =>
                new PolygonPoint(p.Lx * cosT - p.Ly * sinT + node.X,
                    p.Lx * sinT + p.Ly * cosT + node.Y))
            .ToList());

        var dir = new Vector2((float)cosT, (float)sinT);
        var ctx = new MarginContext(node, null, null, null, agvData, dir);
        var polygon = margins.Aggregate(rawPolygon, (p, m) => m.Apply(p, ctx));

        return new ResourceBoundingBox
        {
            Id = Guid.NewGuid(),
            ResourceId = node.Id,
            ResourceType = ResourceType.Node,
            AgvType = agvData.Type,
            Polygon = polygon,
            CalculatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Calculates the bounding box for a single edge.
    /// </summary>
    public ResourceBoundingBox CalculateEdge(GraphEdge edge, AgvData agvData, IReadOnlyList<IMarginStrategy> margins)
    {
        var fromNode = _nodesById[edge.FromNodeId];
        var toNode = _nodesById[edge.ToNodeId];
        var provider = SelectProvider(edge.Variant);
        var polygon = provider.GetPolygon(edge, fromNode, toNode, agvData, margins);

        return new ResourceBoundingBox
        {
            Id = Guid.NewGuid(),
            ResourceId = edge.Id,
            ResourceType = ResourceType.Edge,
            AgvType = agvData.Type,
            Polygon = polygon,
            CalculatedAt = DateTime.UtcNow
        };
    }
}