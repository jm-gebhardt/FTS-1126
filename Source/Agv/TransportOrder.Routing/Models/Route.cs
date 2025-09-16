using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Routing.Models;

/// <summary>
/// Represents the route costs.
/// </summary>
public class Route : IComparable<Route>
{
    /// <inheritdoc cref="Route"/>>
    public Route(double costs, GraphNode[] nodes, Guid[] edges)
    {
        Costs = costs;
        Nodes = nodes;
        Edges = edges;
    }

    /// <summary>
    /// A comparable value representing some kind of costs this route will take.
    /// </summary>
    [PublicAPI]
    public double Costs { get; set; }

    /// <summary>
    /// List of node ids that define the route
    /// </summary>
    [PublicAPI]
    public GraphNode[] Nodes { get; set; }

    /// <summary>
    /// List of edge ids that define the route
    /// </summary>
    [PublicAPI]
    public Guid[] Edges { get; set; }

    /// <summary>
    /// Used for sorting, nulls last.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(Route? other)
    {
        if (other != null)
        {
            if (Costs < other.Costs)
            {
                return -1;
            }
            if (Costs > other.Costs)
            {
                return 1;
            }
            return 0;
        }
        return 2;
    }

    /// <summary>
    /// Combine two routes
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static Route operator +(Route a, Route b)
    {
        GraphNode[] nodes = a.Nodes.LastOrDefault()?.Id == b.Nodes.FirstOrDefault()?.Id ?
            a.Nodes.Concat(b.Nodes.Skip(1)).ToArray() // Skip first item of b if first and last are equal
            :
            a.Nodes.Concat(b.Nodes).ToArray();

        Guid[] edges = a.Edges.LastOrDefault() == b.Edges.FirstOrDefault() ?
            a.Edges.Concat(b.Edges.Skip(1)).ToArray() // Skip first item of b if first and last are equal
            :
            a.Edges.Concat(b.Edges).ToArray();

        return new Route(a.Costs + b.Costs, nodes, edges);
    }
}