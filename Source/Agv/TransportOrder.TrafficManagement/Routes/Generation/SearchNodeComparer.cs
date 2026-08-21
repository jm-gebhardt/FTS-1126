using System.Diagnostics;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Generation;

/// <summary>
/// Compares two SearchNodes for domination checking. Used to determine if one SearchNode dominates another in the search process.
/// </summary>
internal class SearchNodeComparer : IEqualityComparer<SearchNode>
{
    /// <summary>
    /// Determines whether two SearchNodes are equal in terms of their routing properties.
    /// </summary>
    public bool Equals(SearchNode? x, SearchNode? y)
    {
        if (x is null || y is null)
        {
            return false;
        }
        return x.Node.Id == y.Node.Id;
    }

    /// <summary>
    /// Returns a hash code for the specified SearchNode based on its routing properties.
    /// </summary>
    public int GetHashCode(SearchNode obj)
    {
        return obj.Node.Id.GetHashCode();
    }

    /// <summary>
    /// Determines whether the first SearchNode dominates the second SearchNode based on their costs.
    /// </summary>
    public bool FirstDominatesSecond(SearchNode first, SearchNode second)
    {
        Debug.Assert(Equals(first, second));

        return first.TotalCost <= second.TotalCost;
    }
}