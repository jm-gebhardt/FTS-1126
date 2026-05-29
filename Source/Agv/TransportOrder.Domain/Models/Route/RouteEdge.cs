using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Route;

/// <summary>Wraps a graph edge with its release state.</summary>
[PublicAPI]
public class RouteEdge
{
    /// <summary>The graph edge.</summary>
    public GraphEdge GraphEdge { get; }

    /// <summary>Indicates whether this edge is part of the base segment.</summary>
    public bool Released { get; private set; }

    /// <inheritdoc cref="RouteEdge"/>
    public RouteEdge(GraphEdge graphEdge, bool released)
    {
        GraphEdge = graphEdge;
        Released = released;
    }

    /// <summary>Releases this edge into the base segment.</summary>
    public void Release() => Released = true;
}