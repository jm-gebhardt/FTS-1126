namespace Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;

/// <summary>
/// Graph with nodes and edges.
/// </summary>
public interface IGraph
{
    /// <summary>
    /// The list of nodes in the graph.
    /// </summary>
    ICollection<GraphNode> Nodes { get; }

    /// <summary>
    /// The list of edges in the graph.
    /// </summary>
    ICollection<GraphEdge> Edges { get; }
}