using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Route;

/// <summary>Wraps a graph node with its release state.</summary>
[PublicAPI]
public class RouteNode
{
    /// <summary>The graph node.</summary>
    public GraphNode GraphNode { get; }

    /// <summary>Indicates whether this node is part of the base segment.</summary>
    public bool Released { get; private set; }

    /// <inheritdoc cref="RouteNode"/>
    public RouteNode(GraphNode graphNode, bool released)
    {
        GraphNode = graphNode;
        Released = released;
    }

    /// <summary>Releases this node into the base segment.</summary>
    public void Release() => Released = true;
}