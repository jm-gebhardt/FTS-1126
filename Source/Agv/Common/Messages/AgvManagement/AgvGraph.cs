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
        public Guid CorrelationId { get; init; }

        /// <summary>
        /// The Guid of the related map 
        /// </summary>
        public Guid RelatedMapId { get; init; }

        /// <summary>
        /// The name of the related map (map ID)
        /// </summary>
        public string RelatedMapName { get; init; }

        /// <summary>
        /// The list of nodes in the graph.
        /// </summary>
        public ICollection<GraphNode> Nodes { get; init; }

        /// <summary>
        /// The list of edges in the graph.
        /// </summary>
        public ICollection<GraphEdge> Edges { get; init; }

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
        public AgvGraph(Guid correlationId, ICollection<GraphNode> nodes, ICollection<GraphEdge> edges, Guid relatedMapId, string relatedMapName, GraphUsability usability = GraphUsability.Valid)
        {
            Nodes = nodes;
            Edges = edges;
            Usability = usability;
            CorrelationId = correlationId;
            RelatedMapId = relatedMapId;
            RelatedMapName = relatedMapName;
        }

        /// <summary>
        /// Deserialization constructor
        /// </summary>
        private AgvGraph()
        {
            RelatedMapName = null!;
            Nodes = null!;
            Edges = null!;
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
        public enum NodeType
        {
            /// <summary>General WayPoint</summary>
            [JetBrains.Annotations.UsedImplicitly]
            WayPoint,

            /// <summary>Charging station</summary>
            [JetBrains.Annotations.UsedImplicitly]
            Charge,

            /// <summary>Pick station</summary>
            [JetBrains.Annotations.UsedImplicitly]
            Pick,

            /// <summary>Drop station</summary>
            [JetBrains.Annotations.UsedImplicitly]
            Drop,

            /// <summary>Pick/Drop station</summary>
            [JetBrains.Annotations.UsedImplicitly]
            PickDrop,

            /// <summary>Waiting point</summary>
            [JetBrains.Annotations.UsedImplicitly]
            Park
        }
    }
}