using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using MassTransit;
using static Gebhardt.StoreWare.Agv.TransportOrder.Web.Dtos.Graph;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.Web.Dtos
{
    /// <summary>
    /// Model for drawing a graph in the UI
    /// </summary>
    /// <param name="Nodes">The list of nodes in the graph</param>
    /// <param name="Edges">The list of edges in the graph.</param>
    /// <param name="CorrelationId">CorrelationId</param>
    [JetBrains.Annotations.PublicAPI]
    public record Graph(List<AgvGraph.Node> Nodes, List<Edge> Edges, Guid CorrelationId) : CorrelatedBy<Guid>
    {
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
