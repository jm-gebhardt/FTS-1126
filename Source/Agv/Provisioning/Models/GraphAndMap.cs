using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Provisioning.Models;

/// <summary>
/// Contains the map and graph data.
/// </summary>
[UsedImplicitly]
internal class GraphAndMap
{
    /// <summary>
    /// The Graph.
    /// </summary>
    public Common.Models.Graphs.Graph Graph { get; }

    /// <summary>
    /// The Map.
    /// </summary>
    public List<Map> Maps { get; }

    /// <inheritdoc cref="GraphAndMap"/>
    /// <param name="graph">The graph.</param>
    /// <param name="maps">The map.</param>
    public GraphAndMap(Common.Models.Graphs.Graph graph, List<Map> maps)
    {
        Graph = graph;
        Maps = maps;
    }
}