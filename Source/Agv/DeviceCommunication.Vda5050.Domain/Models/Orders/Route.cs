using Gebhardt.Acc.Common.Domain.Models.Errors;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Errors;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

/// <summary>
/// Represents a route of an agv.
/// </summary>
[PublicAPI]
public class Route
{
    /// <summary>
    /// Represents a route of an agv.
    /// </summary>
    /// <param name="nodes">The nodes of the route.</param>
    /// <param name="edges">The edges of the route.</param>
    public Route(ICollection<OrderNode> nodes, ICollection<OrderEdge> edges)
    {
        Nodes = nodes;
        Edges = edges;
    }

    /// <summary>The nodes of the route.</summary>
    public ICollection<OrderNode> Nodes { get; init; }

    /// <summary>The edges of the route.</summary>
    public ICollection<OrderEdge> Edges { get; init; }

    /// <summary>
    /// True, if the route is empty, has no edges.
    /// </summary>
    public bool IsEmpty => Edges.Count == 0;

    /// <summary>
    /// True if the route is valid.
    /// </summary>
    public bool IsValid => Edges.Count == Nodes.Count - 1;

    /// <summary>
    /// Deconstruct the route.
    /// </summary>
    /// <param name="nodes">The nodes of the route.</param>
    /// <param name="edges">The nodes of the route.</param>
    public void Deconstruct(out ICollection<OrderNode> nodes, out ICollection<OrderEdge> edges)
    {
        nodes = Nodes;
        edges = Edges;
    }

    /// <summary>
    /// Subsample a part of the route.
    /// </summary>
    /// <param name="stitchingNodeSequenceId">The sequence id of the start node.</param>
    /// <param name="maximumNodeCount">Maximum number of nodes.</param>
    /// <returns>A new Route starting from the stitching node until the end of the route or until the route has the maximum given number of nodes (at least 2).</returns>
    public Route Subsample(int stitchingNodeSequenceId, int maximumNodeCount)
    {
        var stitchingNode = Nodes.Single(n => n.SequenceId == stitchingNodeSequenceId);
        List<OrderNode> nodes = Nodes.Where(n => n.SequenceId >= stitchingNodeSequenceId && n.SequenceId <= stitchingNodeSequenceId + (maximumNodeCount - 1) * 2 ).ToList();
        var firstEdge = Edges.OrderBy(e => e.SequenceId).First(e => e.StartNodeId == stitchingNode.NodeId);
        List<OrderEdge> edges = Edges.Where(e => e.SequenceId >= firstEdge.SequenceId && e.SequenceId <= nodes.Last().SequenceId - 1).ToList();
        return new Route(nodes, edges);
    }

    /// <summary>
    /// Release the route.
    /// </summary>
    public void Release()
    {
        Nodes.ToList().ForEach(n => n.Release());
        Edges.ToList().ForEach(e => e.Release());
    }

    /// <summary>
    /// Stitch the route to the given route.
    /// </summary>
    /// <param name="route">The route to extend it by.</param>
    public Route Stitch(Route route)
    {
        ICollection<OrderEdge> edges = [.. Edges, .. route.Edges];
        ICollection<OrderNode> nodes = [.. Nodes, .. route.Nodes.Skip(1)];
        return new Route(nodes, edges);
    }

    /// <summary>
    /// TryProceed on the route.
    /// </summary>
    /// <param name="passedNodeSequenceId">The sequence id of the node that has been passed.</param>
    /// <returns>False, if there was no progress, or the passed node is not on this route.</returns>
    public Result Proceed(int passedNodeSequenceId)
    {
        if (passedNodeSequenceId == 0)
        {
            // Ensure that start and end node of current edge are part of the route.
            return Result.Success();
        }
        var previousNode = Nodes.FirstOrDefault(n => n.SequenceId == passedNodeSequenceId - 2) ;
        if (previousNode == null)
        {
            return Result.Failure(OrderErrors.ProceedNodeNotOnRoute);
        }

        var passedEdge = Edges.FirstOrDefault(e => e.StartNodeId == previousNode.NodeId);
        if (passedEdge == null)
        {
            return Result.Failure(OrderErrors.ProceedEdgeNotFound);
        }
        Nodes.Remove(previousNode);
        Edges.Remove(passedEdge);
        return Result.Success();
    }

    /// <summary>
    /// Shift all sequence ids by offset.
    /// </summary>
    /// <param name="offset">The offset.</param>
    public void ShiftSequenceIds(int offset)
    {
        foreach (var orderEdge in Edges)
        {
            orderEdge.SequenceId += offset;
        }
        foreach (var orderNode in Nodes)
        {
            orderNode.SequenceId += offset;
        }
    }

    /// <summary>
    /// Replace all edges and nodes starting from the new routes sequence ids.
    /// </summary>
    /// <param name="newRoute">The new route part.</param>
    public Route Replace(Route newRoute)
    {
        ICollection<OrderEdge> edges = [.. Edges.Where(e => e.SequenceId < newRoute.Edges.Min(nE => nE.SequenceId)), .. newRoute.Edges];
        ICollection<OrderNode> nodes = [.. Nodes.Where(n => n.SequenceId < newRoute.Nodes.Min(nN => nN.SequenceId)), .. newRoute.Nodes];
        return new Route(nodes, edges);
    }

    /// <summary>
    /// Replace all edges and nodes that are before the given node id.
    /// </summary>
    /// <param name="newFirstNode">The first node to keep.</param>
    public Route RemoveBeforeNode(OrderNode newFirstNode)
    {
        var newFirstEdge = Edges.Single(e => e.SequenceId == newFirstNode.SequenceId + 1 && e.StartNodeId == newFirstNode.NodeId);
        var newRoute = new Route(Nodes.Where(n => n.SequenceId >= newFirstNode.SequenceId).ToList(), Edges.Where(e => e.SequenceId >= newFirstEdge.SequenceId).ToList());
        newRoute.ShiftSequenceIds(-newFirstNode.SequenceId);
        return newRoute;
    }

    /// <summary>
    /// Get the base of the route (released nodes and edges.)
    /// </summary>
    public Route GetBase()
    {
        return new Route(Nodes.Where(n => n.Released).OrderBy(n => n.SequenceId).ToList(), Edges.Where(e => e.Released).OrderBy(e => e.SequenceId).ToList());
    }

    /// <summary>
    /// Get the base of the route (not released nodes and edges.)
    /// </summary>
    public Route GetHorizon()
    {
        return new Route(Nodes.Where(n => !n.Released).OrderBy(n => n.SequenceId).ToList(), Edges.Where(e => !e.Released).OrderBy(e => e.SequenceId).ToList());
    }
}