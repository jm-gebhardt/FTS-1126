using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Testing.Helpers;

/// <summary>
/// Helper class to generate a graph for testing.
/// </summary>
[PublicAPI]
public static class GraphGenerator
{
    /// <summary>
    /// Generate default graph
    /// </summary>
    /// <returns></returns>
    public static Graph CreateDefaultGraph()
    {
        return AgvGraphGenerator.CreateDefaultGraph().ToGraph();
    }

    /// <summary>
    /// Generates a graph from the given nodes and edges.
    /// </summary>
    public static Graph Generate(ICollection<GraphNode>? nodes = null, ICollection<GraphEdge>? edges = null)
    {
        return new Graph
        {
            Nodes = nodes ?? [],
            Edges = edges ?? []
        };
    }
}