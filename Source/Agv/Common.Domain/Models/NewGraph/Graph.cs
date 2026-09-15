
namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Represents the global graph consisting of nodes, edges and stations.
/// </summary>
/// <param name="Nodes">The nodes of the graph.</param>
/// <param name="Edges">The edges connecting the nodes.</param>
/// <param name="Stations">The stations of the graph.</param>
public record Graph(IReadOnlyCollection<Node> Nodes, IReadOnlyCollection<Edge> Edges, IReadOnlyCollection<Station> Stations);