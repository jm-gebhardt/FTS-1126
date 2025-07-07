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
            var existing = Edges.SingleOrDefault(e => e.Id == edge.Id);

            if (existing?.Equals(edge) == true ||
                (existing != null && existing.TimeStamp >= edge.TimeStamp))
            {
                continue;
            }

            if (existing != null)
            {
                Edges.Remove(existing);
            }

            Edges.Add(edge);
            hasChanged = true;
        }

        foreach (var node in graph.Nodes)
        {
            var existing = Nodes.SingleOrDefault(n => n.Id == node.Id);

            if (existing?.Equals(node) == true ||
                (existing != null && existing.TimeStamp >= node.TimeStamp))
            {
                continue;
            }

            if (existing != null)
            {
                Nodes.Remove(existing);
            }

            Nodes.Add(node);
            hasChanged = true;
        }

        return hasChanged;
    }
}