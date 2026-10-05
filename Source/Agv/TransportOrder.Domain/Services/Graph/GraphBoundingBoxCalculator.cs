using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;
using GraphNode = Gebhardt.Acc.Common.Domain.Models.NewGraph.Node;
using RoutingGraph = Gebhardt.Acc.Common.Domain.Models.NewGraph.Graph;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

/// <summary>
/// Computes oriented bounding box polygons for every routing node and routing edge in a graph for a given AGV type.
/// </summary>
[PublicAPI]
public class GraphBoundingBoxCalculator
{
    private readonly IReadOnlyList<IEdgePolygonProvider> _providers;
    private readonly IReadOnlyDictionary<Guid, GraphNode> _nodesByRoutingNodeId;
    private readonly IReadOnlyList<RoutingNode> _routingNodes;
    private readonly IReadOnlyList<RoutingEdge> _routingEdges;
    private readonly IReadOnlyDictionary<Guid, IReadOnlyList<RoutingEdge>> _incidentEdgesByRoutingNodeId;

    /// <inheritdoc cref="GraphBoundingBoxCalculator"/>
    public GraphBoundingBoxCalculator(RoutingGraph graph, IEnumerable<IEdgePolygonProvider> providers)
    {
        _providers = providers.ToList();
        _nodesByRoutingNodeId = graph.Nodes
            .SelectMany(n => n.RoutingNodes, (node, routingNode) => (routingNode.Id, Node: node))
            .ToDictionary(pair => pair.Id, pair => pair.Node);
        _routingNodes = graph.Nodes.SelectMany(n => n.RoutingNodes).ToList();
        _routingEdges = graph.Edges.SelectMany(e => e.RoutingEdges)
            .Where(IsNodeToNode)
            .ToList();
        _incidentEdgesByRoutingNodeId = BuildIncidentEdges(_routingEdges);
    }

    private static IReadOnlyDictionary<Guid, IReadOnlyList<RoutingEdge>> BuildIncidentEdges(IReadOnlyList<RoutingEdge> routingEdges)
    {
        var incidentEdges = new Dictionary<Guid, List<RoutingEdge>>();

        foreach (var routingEdge in routingEdges)
        {
            AddIncidentEdge(incidentEdges, routingEdge.StartNodeId, routingEdge);
        }

        foreach (var routingEdge in routingEdges)
        {
            AddIncidentEdge(incidentEdges, routingEdge.EndNodeId, routingEdge);
        }

        return incidentEdges.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<RoutingEdge>)pair.Value);
    }

    private static void AddIncidentEdge(Dictionary<Guid, List<RoutingEdge>> incidentEdges, Guid routingNodeId, RoutingEdge routingEdge)
    {
        if (!incidentEdges.TryGetValue(routingNodeId, out var edges))
        {
            edges = [];
            incidentEdges[routingNodeId] = edges;
        }

        edges.Add(routingEdge);
    }

    private bool IsNodeToNode(RoutingEdge edge)
    {
        return _nodesByRoutingNodeId.ContainsKey(edge.StartNodeId) && _nodesByRoutingNodeId.ContainsKey(edge.EndNodeId);
    }

    private IEdgePolygonProvider SelectProvider(Trajectory? trajectory)
    {
        return _providers.First(p => p.Supports(trajectory));
    }

    private GraphNode GetRequiredNode(Guid routingNodeId)
    {
        if (!_nodesByRoutingNodeId.TryGetValue(routingNodeId, out var node))
        {
            throw new InvalidOperationException($"Routing node {routingNodeId} is not attached to a graph node.");
        }

        return node;
    }

    private double GetOrientation(RoutingNode routingNode)
    {
        var incidentEdge = _incidentEdgesByRoutingNodeId.GetValueOrDefault(routingNode.Id, []).FirstOrDefault();
        if (incidentEdge is null)
        {
            return routingNode.Theta;
        }

        return SelectProvider(incidentEdge.Trajectory)
            .GetDirectionAtEndpoint(incidentEdge, GetRequiredNode(incidentEdge.StartNodeId), GetRequiredNode(incidentEdge.EndNodeId));
    }

    /// <summary>
    /// Calculates bounding boxes for all routing nodes and routing edges of the given AGV type.
    /// </summary>
    public IReadOnlyList<ResourceBoundingBox> CalculateAll(IAgvType agvType, IReadOnlyList<IMarginStrategy> margins)
    {
        var agvTypeId = AgvType.IdFromIdentifier(agvType.AgvTypeIdentifier);
        var results = new List<ResourceBoundingBox>(_routingNodes.Count + _routingEdges.Count);

        foreach (var routingNode in _routingNodes.Where(n => n.AgvTypeId == agvTypeId))
        {
            results.Add(CalculateNode(routingNode, agvType, margins));
        }

        foreach (var routingEdge in _routingEdges.Where(e => e.AgvTypeId == agvTypeId))
        {
            results.Add(CalculateEdge(routingEdge, agvType, margins));
        }

        return results;
    }

    /// <summary>
    /// Calculates the bounding box for a single routing node. The box is oriented along the node's first incident edge, isolated nodes fall back to their theta.
    /// </summary>
    /// <exception cref="InvalidOperationException">The routing node is not attached to a graph node.</exception>
    public ResourceBoundingBox CalculateNode(RoutingNode routingNode, IAgvType agvType, IReadOnlyList<IMarginStrategy> margins)
    {
        var node = GetRequiredNode(routingNode.Id);

        double hL = agvType.Length / 2;
        double hW = agvType.Width / 2;
        double theta = GetOrientation(routingNode);
        double cosT = Math.Cos(theta);
        double sinT = Math.Sin(theta);

        // Local corners in CCW order: back-right, front-right, front-left, back-left.
        (double Lx, double Ly)[] local = [(-hL, -hW), (hL, -hW), (hL, hW), (-hL, hW)];
        var rawPolygon = new Polygon(local.Select(p =>
                new PolygonPoint(p.Lx * cosT - p.Ly * sinT + node.X,
                    p.Lx * sinT + p.Ly * cosT + node.Y))
            .ToList());

        var polygon = margins.Aggregate(rawPolygon, (p, m) => m.Apply(p));

        return new ResourceBoundingBox
        {
            Id = Guid.NewGuid(),
            ResourceId = routingNode.Id,
            ResourceType = ResourceType.Node,
            AgvType = agvType.AgvTypeIdentifier,
            Polygon = polygon,
            CalculatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Calculates the bounding box for a single routing edge.
    /// </summary>
    public ResourceBoundingBox CalculateEdge(RoutingEdge routingEdge, IAgvType agvType, IReadOnlyList<IMarginStrategy> margins)
    {
        var polygon = SelectProvider(routingEdge.Trajectory)
            .GetPolygon(routingEdge, GetRequiredNode(routingEdge.StartNodeId), GetRequiredNode(routingEdge.EndNodeId), agvType, margins);

        return new ResourceBoundingBox
        {
            Id = Guid.NewGuid(),
            ResourceId = routingEdge.Id,
            ResourceType = ResourceType.Edge,
            AgvType = agvType.AgvTypeIdentifier,
            Polygon = polygon,
            CalculatedAt = DateTime.UtcNow
        };
    }
}