using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement
{
    /// <summary>
    /// AGV Graph
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public record AgvGraph : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// The Guid of the related map 
        /// </summary>
        public Guid RelatedMapId { get; }

        /// <summary>
        /// The name of the related map (map ID)
        /// </summary>
        public string RelatedMapName { get; }

        /// <summary>
        /// The list of nodes in the graph.
        /// </summary>
        public Node[] Nodes { get; }

        /// <summary>
        /// The list of edges in the graph.
        /// </summary>
        public Edge[] Edges { get; }

        /// <summary>
        /// Attribute that informs for the usability of the graph.
        /// Only when returning <see cref="GraphUsability.Valid"/> the graph should be used for routing.
        /// </summary>
        public GraphUsability Usability { get; set; }


        /// <summary>
        /// Agv Graph
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="nodes"></param>
        /// <param name="edges"></param>
        /// <param name="relatedMapId"></param>
        /// <param name="relatedMapName"></param>
        /// <param name="usability">Default is <see cref="GraphUsability.Valid"/>.</param>
        public AgvGraph(Guid correlationId, Node[] nodes, Edge[] edges, Guid relatedMapId, string relatedMapName, GraphUsability usability = GraphUsability.Valid)
        {
            CorrelationId = correlationId;
            Nodes = nodes;
            Edges = edges;
            RelatedMapId = relatedMapId;
            RelatedMapName = relatedMapName;
            Usability = usability;
        }


        /// <summary>
        /// Values for graph usability.
        /// </summary><remarks>
        /// On multiple reasons the first on is noted in the subsequent order</remarks>
        [JetBrains.Annotations.PublicAPI]
        public enum GraphUsability
        {
            /// <summary>
            /// Graph is valid and can be used for routing
            /// </summary>
            Valid,

            /// <summary>
            /// Serial number is null or empty
            /// </summary>
            SerialNumberMissing,

            /// <summary>
            /// Invalid number of nodes or nodes not unique
            /// </summary>
            InvalidNodes,

            /// <summary>
            /// Invalid number of edges or edges not unique
            /// </summary>
            InvalidEdges,

            /// <summary>
            /// Serial number of graph does not match to serial number of station list
            /// </summary>
            SerialNumberMismatch,

            /// <summary>
            /// Invalid number of stations or station name not unique
            /// </summary>
            InvalidStations,

            /// <summary>
            /// Station names ambiguous compared to node names
            /// </summary>
            AmbiguousStationNames,

            /// <summary>
            /// Auto-generated edges between stations and graph nodes conflicts to edges from graph
            /// </summary>
            StationEdgeMismatch
        };

        /// <summary>
        /// Node types.
        /// <para>
        /// These types are coming from the specification.
        /// </para>
        /// </summary>
        [Flags]
        public enum NodeType : short
        {
            /// <summary>Charging station</summary>
            [JetBrains.Annotations.UsedImplicitly]
            Charge = 0x0001,

            /// <summary>General waypoint</summary>
            [JetBrains.Annotations.UsedImplicitly]
            WayPoint = 0x0002,

            /// <summary>Rotation point</summary>
            [JetBrains.Annotations.UsedImplicitly]
            Turn = 0x0004,

            /// <summary>Source or destination</summary>
            [JetBrains.Annotations.UsedImplicitly]
            Station = 0x0008,

            /// <summary>Waiting point</summary>
            [JetBrains.Annotations.UsedImplicitly]
            Park = 0x0010
        }

        /// <summary>
        /// Node representation.
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public class Node
        {
            /// <summary>
            /// The unique ID of the node.
            /// </summary><remarks>
            /// Usually this is the (numeric) ID passed by the AGV vendor.
            /// For internal reasons it can be necessary to build/combine an internal identification.
            /// </remarks>
            public string Id { get; set; }

            /// <summary>
            /// Node enabled/disabled.
            /// </summary>
            public bool Enabled { get; set; } = true;

            /// <summary>
            /// Node type of the node.
            /// </summary>
            public NodeType Type { get; set; }

            /// <summary>
            /// The x coordinate of the node.
            /// </summary>
            public float X { get; set; }

            /// <summary>
            /// The y coordinate of the node
            /// </summary>
            public float Y { get; set; }

            /// <summary>
            /// The z coordinate of the node.
            /// <para>
            /// Set to default 0 when not needed.
            /// </para></summary>
            public float Z { get; set; }

            /// <summary>
            /// Simple string representation.
            /// </summary>
            /// <returns></returns>
            public override string ToString() => $"{Id} [X={X}/Y={Y}/Z={Z}]";

            /// <summary>
            /// Node Constructor
            /// </summary>
            /// <param name="id"></param>
            public Node(string id)
            {
                Id = id;
            }
        }

        /// <summary>
        /// Edge representation
        /// </summary>
        [JetBrains.Annotations.PublicAPI]
        public class Edge : ICloneable
        {
            /// <summary>
            /// The ID of the node where the edge starts.
            /// </summary>
            public string From { get; set; }
            /// <summary>
            /// The ID of the node where the edge ends.
            /// </summary>
            public string To { get; set; }

            /// <summary>
            /// Distance/costs to travel this edge (unitless quantifier).
            /// </summary>
            public float Metric { get; set; }

            /// <summary>
            /// Edge Constructor
            /// </summary>
            /// <param name="from"></param>
            /// <param name="to"></param>
            public Edge(string from, string to)
            {
                From = from;
                To = to;
            }

            /// <summary>
            /// Deep copy.
            /// </summary>
            /// <returns></returns>
            public object Clone()
            {
                return new Edge(this.From, this.To)
                {
                    Metric = this.Metric
                };
            }

            /// <summary>
            /// Simple string representation.
            /// </summary>
            /// <returns></returns>
            public override string ToString() => $"{From} -- {Metric} --> {To}";
        }
    }
}
