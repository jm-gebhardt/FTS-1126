using Gebhardt.Acc.Common.Domain.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Provisioning.Models;

/// <summary>
/// Contains the map and graph data.
/// </summary>
[UsedImplicitly]
public class GraphAndMap
{
    /// <summary>
    /// The Graph.
    /// </summary>
    public Graph Graph { get; }

    /// <summary>
    /// The Map.
    /// </summary>
    public List<Map> Maps { get; }

    /// <inheritdoc cref="GraphAndMap"/>
    /// <param name="graph">The graph.</param>
    /// <param name="maps">The map.</param>
    public GraphAndMap(Graph graph, List<Map> maps)
    {
        Graph = graph;
        Maps = maps;
    }
}