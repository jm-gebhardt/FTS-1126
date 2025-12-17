using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.TransportOrder.Test.Extensions;

/// <summary>
/// Provides extension methods for <see cref="AgvGraph"/>
/// </summary>
public static class AgvGraphExtension
{
    extension (AgvGraph graph)
    {
        public Guid[] GetEdgesForNodes(GraphNode[] nodes)
        {
            var edges = new List<Guid>();
            for (int i = 0; i < nodes.Length -1; i++)
            {
                var startNode = nodes[i];
                var endNode = nodes[i + 1];
                var edge = graph.Edges.First(e => e.FromNodeId == startNode.Id && e.ToNodeId == endNode.Id); // First, as there could be multiple, and it is impossible to tell which is meant. Should be ok for unit testing.
                edges.Add(edge.Id);
            }
            return edges.ToArray();
        }
    }
}