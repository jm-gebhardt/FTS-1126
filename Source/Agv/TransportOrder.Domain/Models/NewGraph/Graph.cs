using JetBrains.Annotations;
using CommonGraph = Gebhardt.Acc.Common.Domain.Models.NewGraph.Graph;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

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
    /// For deserialization.
    /// </summary>
    public Graph()
    {
    }

    /// <summary>
    /// Generates a graph from a <see cref="CommonGraph"/>.
    /// </summary>
    /// <param name="commonGraph">The <see cref="CommonGraph"/>.</param>
    public static Graph FromCommon(CommonGraph commonGraph)
    {
        var graph = new Graph();
        Dictionary<Guid, Node> nodesById = commonGraph.Nodes.Select(Node.FromCommon).ToDictionary(n => n.Id);

        graph.Nodes.AddRange(nodesById.Values);
        graph.Edges.AddRange(commonGraph.Edges.Select(e => Edge.FromCommon(e, nodesById)));
        graph.Stations.AddRange(commonGraph.Stations.Select(s => Station.FromCommon(s, nodesById)));

        return graph;
    }
}