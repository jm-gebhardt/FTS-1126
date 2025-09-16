namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

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
        if (duplicateEdges.Count != 0)
        {
            throw new ArgumentException("Duplicate Edges. Edge Ids must be unique.");
        }

        List<IGrouping<Guid, GraphNode>> duplicateNodes = graph.Nodes.GroupBy(n => n.Id).Where(g => g.Count() > 1).ToList();
        if (duplicateNodes.Count != 0)
        {
            throw new ArgumentException("Duplicate Nodes. Node Ids must be unique.");
        }
    }

    /// <summary>
    /// Get the length [m] of a route given by node ids.
    /// </summary>
    /// <param name="graph">This graph.</param>
    /// <param name="routeNodes">The route (collection of node ids).</param>
    /// <returns>Length of the route [m].</returns>
    /// <exception cref="ArgumentNullException">Ambiguous route.</exception>
    /// <exception cref="InvalidOperationException">If any of the subsequent nodes do not match an edge.</exception>
    public static double GetRouteLength(this IGraph graph, GraphNode[] routeNodes)
    {
        double totalLength = 0.0;
        for (int i = 0; i < routeNodes.Length - 1; i++)
        {
            totalLength += graph.Edges.Single(edge => edge.FromNodeId == routeNodes[i].Id && edge.ToNodeId == routeNodes[i + 1].Id).Length;
        }
        return totalLength;
    }
}