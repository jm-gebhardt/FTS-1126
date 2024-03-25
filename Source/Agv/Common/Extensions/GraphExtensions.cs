using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.Common.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IGraph"/>.
/// </summary>
public static class GraphExtensions
{
    /// <summary>
    /// Validates the graph.
    /// </summary>
    /// <param name="graph"></param>
    /// <exception cref="ArgumentException"></exception>
    public static void Validate(this IGraph graph)
    {
        List<IGrouping<Guid, GraphEdge>> duplicateEdges = graph.Edges.GroupBy(e => e.Id).Where(g => g.Count() > 1).ToList();
        if (duplicateEdges.Any())
        {
            throw new ArgumentException("Duplicate Edges. Edge Ids must be unique.");
        }

        List<IGrouping<string, GraphNode>> duplicateNodes = graph.Nodes.GroupBy(n => n.Id).Where(g => g.Count() > 1).ToList();
        if (duplicateNodes.Any())
        {
            throw new ArgumentException("Duplicate Nodes. Node Ids must be unique.");
        }
    }

    /// <summary>
    /// Get the length [m] of a route given by node ids.
    /// </summary>
    /// <param name="graph">This graph.</param>
    /// <param name="route">The route (collection of node ids).</param>
    /// <returns>Length of the route [m].</returns>
    /// <exception cref="ArgumentNullException">Ambiguous route.</exception>
    /// <exception cref="InvalidOperationException">If any of the subsequent nodes do not match an edge.</exception>
    public static double GetRouteLength(this IGraph graph, string[] route)
    {
        double totalLength = 0.0;
        for (int i = 0; i < route.Length - 1; i++)
        {
            totalLength +=  graph.Edges.Single(edge => edge.FromNodeId == route[i] && edge.ToNodeId == route[i + 1]).Length;
        }
        return totalLength;
    }
}