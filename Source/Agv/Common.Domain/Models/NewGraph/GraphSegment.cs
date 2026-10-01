using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Represents a graph segment consisting of nodes, edges and stations.
/// </summary>
/// <param name="Id">Unique identifier of the graph segment.</param>
/// <param name="Nodes">The nodes of the graph segment.</param>
/// <param name="Edges">The edges connecting the nodes.</param>
/// <param name="Stations">The stations of the graph segment.</param>
public record GraphSegment(Guid Id, IReadOnlyCollection<Node> Nodes, IReadOnlyCollection<Edge> Edges, IReadOnlyCollection<Station> Stations) : Graph(Nodes, Edges, Stations)
{
    /// <summary>
    /// Parameterless constructor for EF Core. Uses mutable lists so EF can populate the Nodes/Edges/Stations navigations.
    /// </summary>
    [UsedImplicitly]
    private GraphSegment() : this(Guid.Empty, new List<Node>(), new List<Edge>(), new List<Station>())
    {
    }
}