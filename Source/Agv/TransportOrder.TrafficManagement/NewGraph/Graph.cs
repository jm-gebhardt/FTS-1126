using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.NewGraph;

/// <summary>
/// Represents a graph consisting of nodes, edges, and stations.
/// </summary>
[PublicAPI]
public class Graph
{
    /// <summary>
    /// Collection of nodes in the graph.
    /// </summary>
    public List<Node> Nodes { get; } = [];

    /// <summary>
    /// Collection of edges in the graph.
    /// </summary>
    public List<Edge> Edges { get; } = [];

    /// <summary>
    /// Collection of stations in the graph.
    /// </summary>
    public List<Station> Stations { get; } = [];

    /// <summary>
    /// Generates a graph from a <see cref="GraphSegment"/>.
    /// </summary>
    /// <param name="graphSegment">The <see cref="GraphSegment"/>.</param>
    public static Graph FromCommon(GraphSegment graphSegment)
    {
        var graph = new Graph();
        Dictionary<Guid, Node> nodesById = graphSegment.Nodes.Select(Node.FromCommon).ToDictionary(n => n.Id);

        graph.Nodes.AddRange(nodesById.Values);
        graph.Edges.AddRange(graphSegment.Edges.Select(e => Edge.FromCommon(e, nodesById)));
        graph.Stations.AddRange(graphSegment.Stations.Select(s => Station.FromCommon(s, nodesById)));

        return graph;
    }
}