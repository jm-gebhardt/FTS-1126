
namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db.Models;

/// <summary>
/// AGV Graph. This model is for EF only
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record EfGraph
{
    /// <summary>
    /// The AgvCorrelationId
    /// </summary>
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
    public ICollection<EfNode> Nodes { get; init; }

    /// <summary>
    /// The list of edges in the graph.
    /// </summary>
    public ICollection<EfEdge> Edges { get; init; }

    /// <summary>
    /// Attribute that informs for the usability of the graph.
    /// Only when returning <see cref="EfGraphUsability.Valid"/> the graph should be used for routing.
    /// </summary>
    public EfGraphUsability Usability { get; set; }

    /// <summary>
    /// Deserialization constructor
    /// </summary>
    private EfGraph()
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
    public enum EfGraphUsability
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
}