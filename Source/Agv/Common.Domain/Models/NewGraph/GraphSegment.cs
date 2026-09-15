namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Represents a graph segment consisting of nodes, edges and stations.
/// </summary>
/// <param name="Nodes">The nodes of the graph segment.</param>
/// <param name="Edges">The edges connecting the nodes.</param>
/// <param name="Stations">The stations of the graph segment.</param>
public record GraphSegment(IReadOnlyCollection<Node> Nodes, IReadOnlyCollection<Edge> Edges, IReadOnlyCollection<Station> Stations);