using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

/// <summary>
/// Helper class to generate a graph for testing.
/// </summary>
public static class GraphGenerator
{
    /// <summary>
    /// Generate default graph
    /// </summary>
    /// <returns></returns>
    public static Graph CreateDefaultGraph() => AgvGraphGenerator.CreateDefaultGraph().ToGraph();
}