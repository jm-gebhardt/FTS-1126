using Gebhardt.StoreWare.Agv.Common.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

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