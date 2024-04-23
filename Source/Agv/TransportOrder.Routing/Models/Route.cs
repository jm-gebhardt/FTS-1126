using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Routing.Models;

/// <summary>
/// Represents the route costs.
/// </summary>
public class Route : IComparable<Route>
{
    /// <summary>
    /// Route Constructor
    /// </summary>
    /// <param name="costs"></param>
    /// <param name="nodes"></param>
    public Route(double costs, string[] nodes)
    {
        Costs = costs;
        Nodes = nodes;
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
    public string[] Nodes { get; set; }

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
        string[] nodes = a.Nodes.LastOrDefault() == b.Nodes.FirstOrDefault() ?
            a.Nodes.Concat(b.Nodes.Skip(1)).ToArray() // Skip first item of b if first and last are equal
            :
            a.Nodes.Concat(b.Nodes).ToArray();

        return new Route(a.Costs + b.Costs, nodes);
    }
}