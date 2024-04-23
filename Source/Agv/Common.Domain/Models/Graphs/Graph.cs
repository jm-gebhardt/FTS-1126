namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
/// Model for the (global) graph
/// </summary>
public class Graph : IGraph
{
    /// <summary>
    /// The list of nodes in the graph.
    /// </summary>
    public ICollection<GraphNode> Nodes { get; init; } = new List<GraphNode>();

    /// <summary>
    /// The list of edges in the graph.
    /// </summary>
    public ICollection<GraphEdge> Edges { get; init; } = new List<GraphEdge>();

    /// <summary>
    /// Merges the given graph into this graph.
    /// </summary>
    /// <param name="graph"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public bool Merge(AgvGraph graph)
    {
        graph.Validate();

        bool hasChanged = false;
        foreach (var edge in graph.Edges)
        {
            var existingEdge = Edges.SingleOrDefault(e => e.Id == edge.Id);
            if (existingEdge is null)
            {
                Edges.Add(edge);
                hasChanged = true;
            }
            else
            {
                if (!existingEdge.Equals(edge))
                {
                    throw new InvalidOperationException($"Edge {edge.Id} already exists in graph but is not equal to the new edge.");
                }
            }
        }

        foreach (var node in graph.Nodes)
        {
            var existingNode = Nodes.SingleOrDefault(n => n.Id == node.Id);
            if (existingNode is null)
            {
                Nodes.Add(node);
                hasChanged = true;
            }
            else
            {
                if (!existingNode.Equals(node))
                {
                    throw new InvalidOperationException($"Node {node.Id} already exists in graph but is not equal to the new node.");
                }
            }
        }

        return hasChanged;
    }
}