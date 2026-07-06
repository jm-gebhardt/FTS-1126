using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

/// <summary>
/// Base class for graph generators.
/// </summary>
public abstract class GraphGeneratorBase : IGraphGenerator
{
    /// <summary>
    /// The map generator
    /// </summary>
    [PublicAPI]
    protected IMapGenerator MapGenerator { get; }

    /// <inheritdoc />
    public string[] Sources { get; }

    /// <inheritdoc />
    public string[] Destinations { get; }

    /// <summary>
    /// The GraphId
    /// </summary>
    protected abstract Guid GraphId { get; }

    /// <inheritdoc />
    public abstract AgvGraph Generate(Guid correlationId);

    /// <inheritdoc />
    public IEnumerable<Map> GenerateMaps()
    {
        var graph = GenerateGraph();
        return GenerateMapsBasedOnGlobalGraph(graph);
    }

    /// <summary>
    /// Generate maps based on full graph.
    /// </summary>
    /// <param name="globalGraph"></param>
    /// <returns></returns>
    protected virtual IEnumerable<Map> GenerateMapsBasedOnGlobalGraph(Graph globalGraph)
    {
        const string globalMapName = "Global";
        var graph1 = new Graph
        {
            Nodes = globalGraph.Nodes,
            Edges = globalGraph.Edges
        };
        yield return MapGenerator.Generate(globalMapName.ToGuid(), globalMapName, graph1);
    }

    /// <inheritdoc cref="GraphGeneratorBase"/>
    /// <param name="sources">The source nodes.</param>
    /// <param name="destinations">The destination nodes.</param>
    /// <param name="mapGenerator">The map generator.</param>
    protected GraphGeneratorBase(string[] sources, string[] destinations, IMapGenerator mapGenerator)
    {
        Sources = sources;
        Destinations = destinations;
        MapGenerator = mapGenerator;
    }

    /// <inheritdoc />
    public Graph GenerateGraph() => Generate(GraphId).ToGraph();
}