using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Graph;

/// <summary>
/// AGV Graph. This model is for EF only
/// </summary>
[PublicAPI]
public record EfAgvGraph
{
    /// <summary>
    /// The AgvCorrelationId
    /// </summary>
    public Guid CorrelationId { get; init; }

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
    private EfAgvGraph()
    {
        Nodes = null!;
        Edges = null!;
    }

    /// <summary>
    /// Values for graph usability.
    /// </summary><remarks>
    /// On multiple reasons the first on is noted in the subsequent order</remarks>
    [PublicAPI]
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
    }
}