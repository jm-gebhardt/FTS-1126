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
}