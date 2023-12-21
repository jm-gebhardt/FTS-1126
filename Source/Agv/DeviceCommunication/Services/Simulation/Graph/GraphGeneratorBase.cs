using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Map;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;

/// <summary>
/// Base class for graph generators.
/// </summary>
public abstract class GraphGeneratorBase : IGraphGenerator
{
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
    public IEnumerable<Common.Models.Graphs.Map> GenerateMaps()
    {
        var graph = GenerateGraph();
        return GenerateMapsBasedOnGlobalGraph(graph);
    }

    /// <summary>
    /// Generate maps based on full graph.
    /// </summary>
    /// <param name="globalGraph"></param>
    /// <returns></returns>
    protected virtual IEnumerable<Common.Models.Graphs.Map> GenerateMapsBasedOnGlobalGraph(Common.Models.Graphs.Graph globalGraph)
    {
        const string allMapName = "Global";
        var graph1 = new Common.Models.Graphs.Graph
        {
            Nodes = globalGraph.Nodes,
            Edges = globalGraph.Edges
        };
        yield return new MapGenerator(graph1).Generate(allMapName.ToGuid(), allMapName);
    }

    /// <inheritdoc cref="GraphGeneratorBase"/>
    /// <param name="sources"></param>
    /// <param name="destinations"></param>
    protected GraphGeneratorBase(string[] sources, string[] destinations)
    {
        Sources = sources;
        Destinations = destinations;
    }

    /// <inheritdoc />
    public Common.Models.Graphs.Graph GenerateGraph() => Generate(GraphId).ToGraph();
}