using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.Web.Models
{
    /// <summary>
    /// Model for drawing a graph in the UI
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record Graph : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// The list of nodes in the graph.
        /// </summary>
        public List<AgvGraph.Node> Nodes { get; }

        /// <summary>
        /// The list of edges in the graph.
        /// </summary>
        public List<Edge> Edges { get; }

        /// <summary>
        /// Graph
        /// </summary>
        /// <param name="agvGraph"></param>
        [JetBrains.Annotations.PublicAPI]
        public Graph(AgvGraph agvGraph)
        {
            CorrelationId = agvGraph.CorrelationId;
            Nodes = new(agvGraph.Nodes);
            Edges = new();

            Dictionary<string, AgvGraph.Node> nodeDict = new();
            foreach (var node in Nodes)
            {
                nodeDict.Add(node.Id, node);
            }

            foreach (var edge in agvGraph.Edges)
            {
                if (nodeDict.TryGetValue(edge.From, out var from) &&
                    nodeDict.TryGetValue(edge.To, out var to))
                {
                    Edges.Add(new(from, to));
                }
            }
        }

        /// <summary>
        /// Model for drawing an edge in the UI
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public class Edge
        {
            /// <summary>
            /// Node where the edge starts.
            /// </summary>
            public AgvGraph.Node From { get; set; }

            /// <summary>
            /// Node where the edge ends.
            /// </summary>
            public AgvGraph.Node To { get; set; }

            /// <summary>
            /// UiEdge Constructor
            /// </summary>
            /// <param name="from"></param>
            /// <param name="to"></param>
            public Edge(AgvGraph.Node from, AgvGraph.Node to)
            {
                From = from;
                To = to;
            }
        }
    }
}
