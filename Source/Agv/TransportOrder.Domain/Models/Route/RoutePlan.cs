using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Route;

/// <summary>Represents the route, split into base and horizon segments.</summary>
[PublicAPI]
public class RoutePlan
{
    /// <summary>The action this plan was built for.</summary>
    public AgvAction Action { get; }

    /// <summary>All route nodes with their release state.</summary>
    public List<RouteNode> Nodes { get; }

    /// <summary>All route edges with their release state.</summary>
    public List<RouteEdge> Edges { get; }

    /// <summary>Released nodes forming base segment.</summary>
    public IReadOnlyList<GraphNode> BaseNodes => Nodes.Where(n => n.Released).Select(n => n.GraphNode).ToList();

    /// <summary>Released edges forming the base segment.</summary>
    public IReadOnlyList<GraphEdge> BaseEdges => Edges.Where(e => e.Released).Select(e => e.GraphEdge).ToList();

    /// <summary>Unreleased nodes forming the horizon segment.</summary>
    public IReadOnlyList<GraphNode> HorizonNodes => Nodes.Where(n => !n.Released).Select(n => n.GraphNode).ToList();

    /// <summary>Unreleased edges forming the horizon segment.</summary>
    public IReadOnlyList<GraphEdge> HorizonEdges => Edges.Where(e => !e.Released).Select(e => e.GraphEdge).ToList();

    private RoutePlan(AgvAction action, List<RouteNode> nodes, List<RouteEdge> edges)
    {
        Action = action;
        Nodes = nodes;
        Edges = edges;
    }

    /// <summary>Creates a route plan from an action with all nodes and edges released as base.</summary>
    public static RoutePlan FromAction(AgvAction action)
    {
        if (action.IsMoveToCoordinate || action.Nodes.Count == 0)
        {
            return new RoutePlan(action, [], []);
        }

        var graphNodes = action.Nodes.ToList();
        var nodes = graphNodes.Select(n => new RouteNode(n, released: true)).ToList();
        var edges = Enumerable.Range(0, graphNodes.Count - 1)
            .Select(i => new RouteEdge(
                new GraphEdge(graphNodes[i].Id, graphNodes[i + 1].Id, 1f, action.Edges.ElementAt(i)),
                released: true))
            .ToList();

        return new RoutePlan(action, nodes, edges);
    }

    /// <summary>Releases unreleased nodes up to and including the specified graph node into the base segment.</summary>
    /// <param name="graphNodeId">The target graph node id (inclusive). Null releases all nodes.</param>
    /// <returns>True if at least one node was released. False if the target was not found or nothing was unreleased.</returns>
    public bool ReleaseUntil(Guid? graphNodeId)
    {
        List<RouteNode> toRelease;
        if (graphNodeId == null)
        {
            toRelease = Nodes.Where(n => !n.Released).ToList();
        }
        else
        {
            int targetIndex = Nodes.FindIndex(n => n.GraphNode.Id == graphNodeId);
            if (targetIndex < 0)
            {
                return false;
            }

            toRelease = Nodes.Take(targetIndex + 1).Where(n => !n.Released).ToList();
        }

        foreach (var node in toRelease)
        {
            int index = Nodes.IndexOf(node);
            if (index > 0)
            {
                Edges[index - 1].Release();
            }

            node.Release();
        }

        return toRelease.Count > 0;
    }
}