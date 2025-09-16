using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.Common.Domain.Utilities;

/// <summary>
/// Collection of utility functions to work with routes/nodes.
/// </summary>
public static class RoutingHelper
{
    /// <summary>
    /// Cut the route given by <paramref name="nodes"/> at the given node bounds into a partial route.
    /// </summary>
    /// <param name="nodes">Node list of complete route.</param>
    /// <param name="sourceNodeId">The node ID at which the partial route starts. This node is kept in the result.
    /// When not set the first node is taken.</param>
    /// <param name="destinationNodeId">The node ID at which the partial route ends. This node is kept in the result.
    /// When not set the last node is taken.</param>
    /// <exception cref="ArgumentException">Empty route or node not found.</exception>
    public static string[] CutNodes(string[] nodes, string? sourceNodeId, string? destinationNodeId)
    {
        if (nodes.Length == 0)
        {
            throw new ArgumentException("Nodes missing (empty route).");
        }

        int sourceIndex = 0;
        int destinationIndex = nodes.Length - 1;

        if (!string.IsNullOrEmpty(sourceNodeId))
        {
            sourceIndex = Array.IndexOf(nodes, sourceNodeId);
        }
        if (sourceIndex >= 0 && !string.IsNullOrEmpty(destinationNodeId))
        {
            destinationIndex = Array.IndexOf(nodes, destinationNodeId, sourceIndex);
        }

        if (sourceIndex == 0 && destinationIndex == nodes.Length - 1)
        {
            // No change at all
            return nodes;
        }
        if (sourceIndex < 0 || destinationIndex < 0)
        {
            throw new ArgumentException("Node not found in route.");
        }

        string[] truncated = new string[destinationIndex - sourceIndex + 1];
        Array.Copy(nodes, sourceIndex, truncated, 0, truncated.Length);
        return truncated;
    }

    /// <summary>
    /// Get the index of the given node in the route.
    /// </summary>
    /// <param name="route">The full route</param>
    /// <param name="nodeId">The node id</param>
    /// <returns></returns>
    public static uint GetFirstIndexOfNodeId(GraphNode[] route, Guid? nodeId)
    {
        int currentIndex = 0;

        if (nodeId != null)
        {
            currentIndex = Array.FindIndex(route, n => n.Id == nodeId);
        }

        if (currentIndex <= 0)
        {
            currentIndex = 0;
        }
        return (uint) currentIndex;
    }

    /// <summary>
    /// Get the remaining route (starting) from the given node
    /// </summary>
    /// <param name="lastNodeId"></param>
    /// <param name="route"></param>
    /// <returns></returns>
    public static GraphNode[] GetRemainingRoute(Guid? lastNodeId, GraphNode[] route)
    {
        uint currentIndex = GetFirstIndexOfNodeId(route, lastNodeId);
        GraphNode[] remainingRoute = route.Skip((int) currentIndex).ToArray();
        return remainingRoute;
    }
}